using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive
{
    public class AzureFileServiceDriveInfo : PSDriveInfo
    {
        public CloudFileClient Client { get; set; }
        public string Endpoint { get;set;}

        private AzureFileServiceDriveInfo(PSDriveInfo driveInfo)
            : base(driveInfo)
        {

        }

        public static AzureFileServiceDriveInfo Parse(PSDriveInfo driveInfo)
        {
            var url = driveInfo.Root;
            var parts = url.Split('?');
            var endpoint = parts[0];
            var dict = ParseValues(parts[1]);
            var accountName = dict["account"];
            var accountKey = dict["key"];

            var cred = new StorageCredentials(accountName, accountKey);
            var account = new CloudStorageAccount(cred, null, null, null, fileStorageUri: new StorageUri(new Uri(endpoint)));
            var client = account.CreateCloudFileClient();

            var info = new PSDriveInfo(name: driveInfo.Name, provider: driveInfo.Provider, root: PathResolver.Root, description: string.Empty, credential: null);

            return new AzureFileServiceDriveInfo(info)
            {
                Client = client,
                Endpoint = endpoint
            };
        }

        private static Dictionary<string, string> ParseValues(string str)
        {
            var dict = new Dictionary<string, string>();
            var sep = new char[] {'='};
            var parts = str.Split('&');
            foreach (var p in parts)
            {
                var pair = p.Split(sep, 2);
                dict.Add(pair[0].ToLowerInvariant(), pair[1]);
            }

            return dict;
        }

        public string ExtractPath(string rawPath)
        {
            var path = rawPath.Split('?')[0];
            return path.Substring(this.Root.Length);
        }

        internal IEnumerable<object> ListItems(string path)
        {
            var result = PathResolver.ResolvePath(this.Client, path, skipCheckExistence: false);

            switch (result.PathType)
            {
                case PathType.AzureFileRoot:
                    return ListShares(this.Client);
                case PathType.AzureFileDirectory:
                    return ListDirectory(result.Directory);
                case PathType.AzureFile:
                    return ListFile(result.File);
                default:
                    return null;
            }
        }

        private IEnumerable<object> ListShares(CloudFileClient client)
        {
            return client.ListShares();
        }

        public void HandleItems(IEnumerable<object> items, Action<CloudFile> fileAction, Action<CloudFileDirectory> dirAction, Action<CloudFileShare> shareAction)
        {
            foreach (var i in items)
            {
                var d = i as CloudFileDirectory;
                if (d != null)
                {
                    dirAction(d);
                    continue;
                }

                var f = i as CloudFile;
                if (f != null)
                {
                    fileAction(f);
                    continue;
                }

                var s = i as CloudFileShare;
                if (s != null)
                {
                    shareAction(s);
                    continue;
                }
            }
        }

        private IEnumerable<IListFileItem> ListFile(CloudFile file)
        {
            return new IListFileItem[] { file };
        }

        private IEnumerable<IListFileItem> ListDirectory(CloudFileDirectory dir)
        {
            var list = dir.ListFilesAndDirectories().ToList();
            return list;
        }

        internal void CreateDirectory(string path)
        {
            var r = PathResolver.ResolvePath(this.Client, path);

            switch (r.PathType)
            {
                case PathType.AzureFileRoot:
                    return;
                case PathType.AzureFileDirectory:
                    CreateDirectoryAndShare(r.Directory);
                    return;
                case PathType.AzureFile:
                    throw new Exception("File " + path + " already exists.");
                default:
                    return;
            }
        }

        internal void CreateDirectoryAndShare(CloudFileDirectory dir)
        {
            var share = dir.Share;
            if (!share.Exists())
            {
                share.Create();
            }

            CreateParentDirectory(dir, share.GetRootDirectoryReference());
            if (!dir.Exists())
            {
                dir.Create();
            }
        }

        private void CreateParentDirectory(CloudFileDirectory dir, CloudFileDirectory rootDir)
        {
            var p = dir.Parent;
            if (p == null || p.Uri == rootDir.Uri)
            {
                return;
            }

            if (p.Exists())
            {
                return;
            }

            CreateParentDirectory(p, rootDir);

            p.Create();
        }

        internal void CreateEmptyFile(string path, long size)
        {
            var file = GetFile(path);
            if (file == null)
            {
                throw new Exception("Path " + path + " is not a valid file path.");
            }

            file.Create(size);
        }

        internal void CreateFile(string path, string content)
        {
            var file = GetFile(path);
            if (file == null)
            {
                throw new Exception("Path " + path + " is not a valid file path.");
            }

            CreateDirectoryAndShare(file.Parent);
            file.UploadText(content);
        }

        internal System.Management.Automation.Provider.IContentReader GetReader(string path)
        {
            var r = PathResolver.ResolvePath(this.Client, path, hint: PathType.AzureFile, skipCheckExistence: false);
            if (r.PathType == PathType.AzureFile)
            {
                var reader = new AzureFileReader(GetFile(path));
                return reader;
            }

            return null;
        }

        public CloudFile GetFile(string path)
        {
            var r = PathResolver.ResolvePath(this.Client, path, hint: PathType.AzureFile);
            if (r.PathType == PathType.AzureFile)
            {
                return r.File;
            }

            return null;
        }

        internal void DeleteDirectory(CloudFileDirectory dir, bool recurse)
        {
            if (dir.Share.GetRootDirectoryReference().Uri == dir.Uri)
            {
                dir.Share.Delete();
                return;
            }

            var items = dir.ListFilesAndDirectories();
            if (recurse)
            {
                HandleItems(items,
                    (f) => f.Delete(),
                    (d) => DeleteDirectory(d, recurse),
                    (s) => s.Delete());

                dir.Delete();
            }
            else
            {
                if (items.Count() == 0)
                {
                    dir.Delete();
                }
                else
                {
                    throw new Exception("The directory is not empty. Please specify -recurse to delete it.");
                }
            }
        }

        internal CloudFileShare CreateShare(string shareName)
        {
            var share = this.Client.GetShareReference(shareName);
            if (!share.Exists())
            {
                share.Create();
                return share;
            }

            throw new Exception("Share " + shareName + " already exists");
        }

        internal void Download(string path, string destination)
        {
            var r = PathResolver.ResolvePath(this.Client, path, skipCheckExistence: false);
            var targetIsDir = Directory.Exists(destination);

            switch (r.PathType)
            {
                case PathType.AzureFile:
                    if (targetIsDir)
                    {
                        destination = PathResolver.Combine(destination, r.Parts.Last());
                    }

                    r.File.DownloadToFile(destination, FileMode.CreateNew);
                    break;
                case PathType.AzureFileDirectory:
                    if (string.IsNullOrEmpty(r.Directory.Name))
                    {
                        //at share level
                        this.DownloadShare(r.Share, destination);
                    }
                    else
                    {
                        DownloadDirectory(r.Directory, destination);
                    }
                    break;
                case PathType.AzureFileRoot:
                    var shares = this.Client.ListShares();
                    foreach (var share in shares)
                    {
                        this.DownloadShare(share, destination);
                    }
                    break;
                default:
                    break;
            }
        }

        private void DownloadShare(CloudFileShare share, string destination)
        {
            destination = PathResolver.Combine(destination, share.Name);
            Directory.CreateDirectory(destination);

            var dir = share.GetRootDirectoryReference();
            var items = dir.ListFilesAndDirectories();

            this.HandleItems(items,
                (f) =>
                {
                    f.DownloadToFile(PathResolver.Combine(destination, f.Name), FileMode.CreateNew);
                },
                (d) =>
                {
                    DownloadDirectory(d, destination);
                },
                (s) => { });
        }

        internal void DownloadDirectory(CloudFileDirectory dir, string destination)
        {
            destination = Path.Combine(destination, dir.Name);
            Directory.CreateDirectory(destination);
            var items = dir.ListFilesAndDirectories();
            this.HandleItems(items,
                (f) =>
                {
                    f.DownloadToFile(PathResolver.Combine(destination, f.Name), FileMode.CreateNew);
                },
                (d) =>
                {
                    DownloadDirectory(d, destination);
                },
                (s) => { });
        }

        internal void Upload(string localPath, string targePath)
        {
            var r = PathResolver.ResolvePath(this.Client, targePath, skipCheckExistence: false);
            var localIsDirectory = Directory.Exists(localPath);
            var local = PathResolver.SplitPath(localPath);
            switch (r.PathType)
            {
                case PathType.AzureFileRoot:
                    if (localIsDirectory)
                    {
                        var share = CreateShare(local.Last());
                        var dir = share.GetRootDirectoryReference();
                        foreach (var f in Directory.GetFiles(localPath))
                        {
                            UploadFile(f, dir);
                        }

                        foreach (var d in Directory.GetDirectories(localPath))
                        {
                            UploadDirectory(d, dir);
                        }
                    }
                    else
                    {
                        throw new Exception("Cannot upload file as file share.");
                    }
                    break;
                case PathType.AzureFileDirectory:
                    if (localIsDirectory)
                    {
                        UploadDirectory(localPath, r.Directory);
                    }
                    else
                    {
                        UploadFile(localPath, r.Directory);
                    }
                    break;
                case PathType.AzureFile:
                default:
                    break;
            }

        }

        private void UploadDirectory(string localPath, CloudFileDirectory dir)
        {
            var localDirName = Path.GetFileName(localPath);
            var subdir = dir.GetDirectoryReference(localDirName);
            subdir.Create();

            foreach (var f in Directory.GetFiles(localPath))
            {
                UploadFile(f, subdir);
            }

            foreach (var d in Directory.GetDirectories(localPath))
            {
                UploadDirectory(d, subdir);
            }
        }

        private void UploadFile(string localFile, CloudFileDirectory dir)
        {
            var file = Path.GetFileName(localFile);
            var f = dir.GetFileReference(file);
            var condition = new AccessCondition();
            f.UploadFromFile(localFile, FileMode.CreateNew);
        }
    }

    class AzureFileReader : IContentReader
    {
        private CloudFile File { get; set; }
        private long length = 0;
        private long offset = 0;
        private const int unit = 80;
        public AzureFileReader(CloudFile file)
        {
            this.File = file;
            this.File.FetchAttributes();
            length = this.File.Properties.Length;
        }
        public void Close()
        {
        }

        public System.Collections.IList Read(long readCount)
        {
            var total = readCount * unit;
            if (offset >= length)
            {
                return null;
            }

            if (offset + total >= length)
            {
                total = length - offset;
            }

            var b = new byte[total];
            this.File.DownloadRangeToByteArray(b, 0, offset, total);
            offset += total;

            var l = new List<string>();
            var fullparts = (int) Math.Floor(total * 1.0 / unit);
            for(var i = 0; i < fullparts; ++i) {
                var s = Encoding.UTF8.GetString(b, i * unit, unit);
                
                l.Add(s);
            }

            //last part
            if (total > unit * fullparts) {
                var s = Encoding.UTF8.GetString(b, fullparts * unit, (int) (total - unit * fullparts));
                l.Add(s);
            }

            return l;

        }

        public void Seek(long offset, System.IO.SeekOrigin origin)
        {
            this.offset = (int) offset;
        }

        public void Dispose()
        {
        }
    }
}

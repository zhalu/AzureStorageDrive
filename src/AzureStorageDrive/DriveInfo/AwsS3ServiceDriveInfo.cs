﻿//using Amazon.Runtime;
//using Amazon.S3;
//using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Auth;
//using Microsoft.WindowsAzure.Storage.Blob;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Management.Automation;
//using System.Management.Automation.Provider;
//using System.Text;
//using System.Threading.Tasks;

//namespace AzureStorageDrive
//{
//    public class AwsS3ServiceDriveInfo : AbstractDriveInfo
//    {
//        public IAmazonS3 Client { get; set; }
//        public string Name { get; set; }

//        public AwsS3ServiceDriveInfo(string url, string name)
//        {
//            var dict = ParseValues(url);
//            var accessKey = dict["accesskey"];
//            var secretKey = dict["secretkey"];

//            var client = Amazon.AWSClientFactory.CreateAmazonS3Client(accessKey, secretKey);

//            this.Client = client;
//            this.Name = name;
//        }

//        public override void NewItem(
//                            string path,
//                            string type,
//                            object newItemValue)
//        {
//            if (string.Equals(type, "Directory", StringComparison.InvariantCultureIgnoreCase))
//            {
//                this.CreateDirectory(path);
//            }
//            else if (string.Equals(type, "EmptyFile", StringComparison.InvariantCultureIgnoreCase))
//            {
//                if (newItemValue != null)
//                {
//                    var size = 0L;
//                    if (long.TryParse(newItemValue.ToString(), out size))
//                    {
//                        this.CreateEmptyFile(path, size);
//                    }
//                    else
//                    {
//                        this.CreateEmptyFile(path, 0);
//                    }
//                }
//            }
//            else
//            {
//                var parts = PathResolver.SplitPath(path);
//                if (parts.Count == 1)
//                {
//                    this.CreateContainer(parts[0]);
//                }
//                else
//                {
//                    this.CreateBlob(path, newItemValue.ToString());
//                }
//            }
//        }

//        public override void GetChildItems(string path, bool recurse)
//        {
//            var folders = recurse ? new List<string>() : null;

//            var items = this.ListItems(path);
//            this.HandleItems(items,
//                (b) =>
//                {
//                    this.RootProvider.WriteItemObject(b, path, true);
//                },
//                (d) =>
//                {
//                    this.RootProvider.WriteItemObject(d, path, true);
//                    if (recurse)
//                    {
//                        var name = PathResolver.SplitPath(d.Prefix).Last();
//                        var p = PathResolver.Combine(path, name);
//                        folders.Add(p);
//                    }
//                },
//                (c) =>
//                {
//                    this.RootProvider.WriteItemObject(c, path, true);
//                    if (recurse)
//                    {
//                        var p = PathResolver.Combine(path, c.Name);
//                        folders.Add(p);
//                    }
//                });

//            if (recurse && folders != null)
//            {
//                foreach (var f in folders)
//                {
//                    GetChildItems(f, recurse);
//                }
//            }
//        }

//        public override void GetChildNames(string path, ReturnContainers returnContainers)
//        {
//            var r = AzureBlobPathResolver.ResolvePath(this.Client, path);
//            switch (r.PathType)
//            {
//                case PathType.AzureBlobRoot:
//                    var shares = this.ListItems(path);
//                    foreach (CloudBlobContainer s in shares)
//                    {
//                        this.RootProvider.WriteItemObject(s.Name, path, true);
//                    }
//                    break;
//                case PathType.AzureBlobDirectory:
//                    ListAndHandle(r.Directory,
//                        blobAction: b => this.RootProvider.WriteItemObject(b.Name, b.Parent.Uri.ToString(), false),
//                        dirAction: d => this.RootProvider.WriteItemObject(d.Prefix, d.Parent.Uri.ToString(), false)
//                        );
//                    break;
//                case PathType.AzureBlobBlock:
//                default:
//                    break;
//            }
//        }

//        public override void RemoveItem(string path, bool recurse)
//        {
//            var r = AzureBlobPathResolver.ResolvePath(this.Client, path, skipCheckExistence: false);
//            switch (r.PathType)
//            {
//                case PathType.AzureBlobDirectory:
//                    this.DeleteDirectory(r.Directory, recurse);
//                    break;
//                case PathType.AzureBlobBlock:
//                    r.Blob.Delete();
//                    break;
//                default:
//                    break;
//            }
//        }

//        internal IEnumerable<object> ListItems(string path)
//        {
//            var result = AzureBlobPathResolver.ResolvePath(this.Client, path, skipCheckExistence: false);

//            switch (result.PathType)
//            {
//                case PathType.AzureBlobRoot:
//                    return ListContainers(this.Client);
//                case PathType.AzureBlobDirectory:
//                    return ListDirectory(result.Directory);
//                case PathType.AzureBlobBlock:
//                    return ListBlob(result.Blob);
//                default:
//                    return null;
//            }
//        }

//        private IEnumerable<object> ListContainers(CloudBlobClient client)
//        {
//            return client.ListContainers();
//        }

//        public bool IsDirEmpty(CloudBlobDirectory dir)
//        {
//            var r = dir.ListBlobsSegmented(true, BlobListingDetails.None, 1, null, null, null);
//            return r.Results.Count() == 0;
//        }

//        public void ListAndHandle(CloudBlobDirectory dir, 
//            bool flatBlobListing = false,
//            Action<ICloudBlob> blobAction = null, 
//            Action<CloudBlobDirectory> dirAction = null,
//            Action<CloudBlobContainer> containerAction = null)
//        {
//            BlobContinuationToken token = null;
//            while (true)
//            {
//                var r = dir.ListBlobsSegmented(flatBlobListing, BlobListingDetails.None, 10, token, null, null);
//                token = r.ContinuationToken;
//                var blobs = r.Results;
//                this.HandleItems(blobs, blobAction, dirAction, containerAction);

//                if (token == null)
//                {
//                    break;
//                }
//            }
//        }

//        public void HandleItems(IEnumerable<object> items, 
//            Action<ICloudBlob> blobAction = null, 
//            Action<CloudBlobDirectory> dirAction = null, 
//            Action<CloudBlobContainer> containerAction = null)
//        {
//            if (items == null)
//            {
//                return;
//            }

//            foreach (var i in items)
//            {
//                var d = i as CloudBlobDirectory;
//                if (d != null)
//                {
//                    dirAction(d);
//                    continue;
//                }

//                var f = i as ICloudBlob;
//                if (f != null)
//                {
//                    blobAction(f);
//                    continue;
//                }

//                var s = i as CloudBlobContainer;
//                if (s != null)
//                {
//                    containerAction(s);
//                    continue;
//                }
//            }
//        }

//        private IEnumerable<IListBlobItem> ListBlob(ICloudBlob file)
//        {
//            return new IListBlobItem[] { file };
//        }

//        private IEnumerable<object> ListDirectory(CloudBlobDirectory dir)
//        {
//            var list = new List<object>();
//            ListAndHandle(dir,
//                blobAction: b => list.Add(b),
//                dirAction: d => list.Add(d));
//            return list;
//        }

//        internal void CreateDirectory(string path)
//        {
//            var r = AzureBlobPathResolver.ResolvePath(this.Client, path);

//            switch (r.PathType)
//            {
//                case PathType.AzureBlobRoot:
//                    return;
//                case PathType.AzureBlobDirectory:
//                    CreateContainer(r.Directory);
//                    return;
//                case PathType.AzureBlobBlock:
//                    throw new Exception("File " + path + " already exists.");
//                default:
//                    return;
//            }
//        }

//        internal void CreateContainer(CloudBlobDirectory dir)
//        {
//            var container = dir.Container;
//            if (!container.Exists())
//            {
//                container.Create();
//            }
//        }

//        internal void CreateEmptyFile(string path, long size)
//        {
//            var file = GetBlob(path, PathType.AzureBlobPage) as CloudPageBlob;
//            if (file == null)
//            {
//                throw new Exception("Path " + path + " is not a valid file path.");
//            }

//            file.Create(size);
//        }

//        internal void CreateBlob(string path, string content)
//        {
//            var file = GetBlob(path, PathType.AzureBlobBlock) as CloudBlockBlob;
//            if (file == null)
//            {
//                throw new Exception("Path " + path + " is not a valid file path.");
//            }

//            CreateContainer(file.Parent);
//            file.UploadText(content);
//        }

//        public override IContentReader GetContentReader(string path)
//        {
//            var r = AzureBlobPathResolver.ResolvePath(this.Client, path, hint: PathType.AzureBlobBlock, skipCheckExistence: false);
//            if (r.PathType == PathType.AzureBlobBlock)
//            {
//                var reader = new AzureBlobReader(GetBlob(path, PathType.Unknown));
//                return reader;
//            }

//            return null;
//        }

//        public ICloudBlob GetBlob(string path, PathType expectedType)
//        {
//            var r = AzureBlobPathResolver.ResolvePath(this.Client, path, hint: expectedType);
//            if (r.PathType == expectedType)
//            {
//                return r.Blob;
//            }

//            return null;
//        }

//        internal void DeleteDirectory(CloudBlobDirectory dir, bool recurse)
//        {
//            if (recurse)
//            {
//                ListAndHandle(dir,
//                    flatBlobListing: true,
//                    blobAction: (b) => b.Delete());
//            }
//            else
//            {
//                if (!IsDirEmpty(dir))
//                {
//                    throw new Exception("The directory is not empty. Please specify -recurse to delete it.");
//                }
//            }
//        }

//        internal CloudBlobContainer CreateContainer(string containerName)
//        {
//            var container = this.Client.GetContainerReference(containerName);
//            if (!container.Exists())
//            {
//                container.Create();
//                return container;
//            }

//            throw new Exception("Container " + containerName + " already exists");
//        }

//        internal void Download(string path, string destination)
//        {
//            var r = AzureBlobPathResolver.ResolvePath(this.Client, path, skipCheckExistence: false);
//            var targetIsDir = Directory.Exists(destination);

//            switch (r.PathType)
//            {
//                case PathType.AzureBlobBlock:
//                    if (targetIsDir)
//                    {
//                        destination = PathResolver.Combine(destination, r.Parts.Last());
//                    }

//                    r.Blob.DownloadToFile(destination, FileMode.CreateNew);
//                    break;
//                case PathType.AzureBlobDirectory:
//                    if (r.Directory == r.RootDirectory)
//                    {
//                        //at container level
//                        this.DownloadContainer(r.Container, destination);
//                    }
//                    else
//                    {
//                        DownloadDirectory(r.Directory, destination);
//                    }
//                    break;
//                case PathType.AzureBlobRoot:
//                    var shares = this.Client.ListContainers();
//                    foreach (var share in shares)
//                    {
//                        this.DownloadContainer(share, destination);
//                    }
//                    break;
//                default:
//                    break;
//            }
//        }

//        private void DownloadContainer(CloudBlobContainer container, string destination)
//        {
//            destination = PathResolver.Combine(destination, container.Name);
//            Directory.CreateDirectory(destination);

//            var dir = container.GetDirectoryReference("");
//            ListAndHandle(dir,
//                flatBlobListing: false,
//                blobAction: (b) => b.DownloadToFile(PathResolver.Combine(destination, b.Name), FileMode.CreateNew),
//                dirAction: (d) => DownloadDirectory(d, destination));
//        }

//        internal void DownloadDirectory(CloudBlobDirectory dir, string destination)
//        {
//            destination = Path.Combine(destination, dir.Prefix);
//            Directory.CreateDirectory(destination);

//            ListAndHandle(dir,
//                flatBlobListing: false,
//                blobAction: (b) => b.DownloadToFile(PathResolver.Combine(destination, b.Name), FileMode.CreateNew),
//                dirAction: (d) => DownloadDirectory(d, destination));
//        }

//        internal void Upload(string localPath, string targePath)
//        {
//            var r = AzureBlobPathResolver.ResolvePath(this.Client, targePath, skipCheckExistence: false);
//            var localIsDirectory = Directory.Exists(localPath);
//            var local = PathResolver.SplitPath(localPath);
//            switch (r.PathType)
//            {
//                case PathType.AzureBlobRoot:
//                    if (localIsDirectory)
//                    {
//                        var container = CreateContainer(local.Last());
//                        var dir = container.GetDirectoryReference("");
//                        foreach (var f in Directory.GetFiles(localPath))
//                        {
//                            UploadFile(f, dir);
//                        }

//                        foreach (var d in Directory.GetDirectories(localPath))
//                        {
//                            UploadDirectory(d, dir);
//                        }
//                    }
//                    else
//                    {
//                        throw new Exception("Cannot upload file as file share.");
//                    }
//                    break;
//                case PathType.AzureBlobDirectory:
//                    if (localIsDirectory)
//                    {
//                        UploadDirectory(localPath, r.Directory);
//                    }
//                    else
//                    {
//                        UploadFile(localPath, r.Directory);
//                    }
//                    break;
//                case PathType.AzureBlobBlock:
//                default:
//                    break;
//            }

//        }

//        private void UploadDirectory(string localPath, CloudBlobDirectory dir)
//        {
//            var localDirName = Path.GetFileName(localPath);
//            var subdir = dir.GetDirectoryReference(localDirName);

//            foreach (var f in Directory.GetFiles(localPath))
//            {
//                UploadFile(f, subdir);
//            }

//            foreach (var d in Directory.GetDirectories(localPath))
//            {
//                UploadDirectory(d, subdir);
//            }
//        }

//        private void UploadFile(string localFile, CloudBlobDirectory dir)
//        {
//            var file = Path.GetFileName(localFile);
//            var f = dir.GetBlockBlobReference(file);
//            var condition = new AccessCondition();
//            f.UploadFromFile(localFile, FileMode.CreateNew);
//        }

//        public override bool HasChildItems(string path)
//        {
//            var r = AzureBlobPathResolver.ResolvePath(this.Client, path, hint: PathType.AzureBlobDirectory, skipCheckExistence: false);
//            return r.Exists();
//        }

//        public override bool IsValidPath(string path)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool ItemExists(string path)
//        {
//            if (PathResolver.IsLocalPath(path))
//            {
//                path = PathResolver.ConvertToRealLocalPath(path);
//                return File.Exists(path) || Directory.Exists(path);
//            }

//            try
//            {
//                var r = AzureBlobPathResolver.ResolvePath(this.Client, path, skipCheckExistence: false);
//                var exists = r.Exists();
//                return exists;
//            }
//            catch (Exception e)
//            {
//                return false;
//            }
//        }

//        public override bool IsItemContainer(string path)
//        {
//            if (PathResolver.IsLocalPath(path))
//            {
//                return true;
//            }

//            var parts = PathResolver.SplitPath(path);
//            if (parts.Count == 0)
//            {
//                return true;
//            }

//            try
//            {
//                var r = AzureBlobPathResolver.ResolvePath(this.Client, path, hint: PathType.AzureBlobDirectory, skipCheckExistence: false);
//                return r.Exists();
//            }
//            catch (Exception e)
//            {
//                return false;
//            }
//        }

//        public override void GetProperty(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList)
//        {
//            var r = AzureBlobPathResolver.ResolvePath(this.Client, path, skipCheckExistence: false);
//            switch (r.PathType)
//            {
//                case PathType.AzureBlobBlock:
//                    r.Blob.FetchAttributes();
//                    this.RootProvider.WriteItemObject(r.Blob.Properties, path, false);
//                    this.RootProvider.WriteItemObject(r.Blob.Metadata, path, false);
//                    break;
//                case PathType.AzureBlobDirectory:
//                    if (r.Directory == r.RootDirectory)
//                    {
//                        r.Container.FetchAttributes();
//                        this.RootProvider.WriteItemObject(r.Container.Properties, path, true);
//                        this.RootProvider.WriteItemObject(r.Container.Metadata, path, true);
//                    }
//                    else
//                    {
//                        //none to show
//                    }
//                    break;
//                default:
//                    break;
//            }
//        }

//        public override void SetProperty(string path, PSObject propertyValue)
//        {
//            var r = AzureBlobPathResolver.ResolvePath(this.Client, path, skipCheckExistence: false);
//            switch (r.PathType)
//            {
//                case PathType.AzureBlobBlock:
//                    r.Blob.FetchAttributes();
//                    MergeProperties(r.Blob.Metadata, propertyValue.Properties);
//                    r.Blob.SetMetadata();
//                    break;
//                case PathType.AzureBlobDirectory:
//                    if (r.Parts.Count() == 1)
//                    {
//                        r.Container.FetchAttributes();
//                        MergeProperties(r.Container.Metadata, propertyValue.Properties);
//                        r.Container.SetMetadata();
//                    }
//                    else
//                    {
//                        throw new Exception("Setting metadata/properties for directory is not supported");
//                    }
//                    break;
//                default:
//                    break;
//            }
//        }

//        private void MergeProperties(IDictionary<string, string> target, PSMemberInfoCollection<PSPropertyInfo> source)
//        {
//            foreach (var info in source)
//            {
//                var name = info.Name;
//                if (target.ContainsKey(name))
//                {
//                    target.Remove(name);
//                }

//                target.Add(name, info.Value.ToString());
//            }
//        }

//        #region Data copy 
//        public bool CopyItem(string localPath, AzureBlobServiceDriveInfo targetDrive, string targetPath, bool recurse)
//        {
//            return false;
//        }

//        public bool CopyItem(AzureBlobServiceDriveInfo drive, string source, string localPath, bool recurse)
//        {
//            return false;
//        }

//        public bool CopyItem(AzureBlobServiceDriveInfo sourceDrive, string sourcePath, AzureFileServiceDriveInfo targetDrive, string targetPath, bool recurse)
//        {
//            return false;
//        }
//        #endregion
//    }

//    class AwsS3Reader : IContentReader
//    {
//        private ICloudBlob File { get; set; }
//        private long length = 0;
//        private long offset = 0;
//        private const int unit = 80;
//        public AwsS3Reader(ICloudBlob file)
//        {
//            this.File = file;
//            this.File.FetchAttributes();
//            length = this.File.Properties.Length;
//        }
//        public void Close()
//        {
//        }

//        public System.Collections.IList Read(long readCount)
//        {
//            var total = readCount * unit;
//            if (offset >= length)
//            {
//                return null;
//            }

//            if (offset + total >= length)
//            {
//                total = length - offset;
//            }

//            var b = new byte[total];
//            this.File.DownloadRangeToByteArray(b, 0, offset, total);
//            offset += total;

//            var l = new List<string>();
//            var fullparts = (int)Math.Floor(total * 1.0 / unit);
//            for (var i = 0; i < fullparts; ++i)
//            {
//                var s = Encoding.UTF8.GetString(b, i * unit, unit);

//                l.Add(s);
//            }

//            //last part
//            if (total > unit * fullparts)
//            {
//                var s = Encoding.UTF8.GetString(b, fullparts * unit, (int)(total - unit * fullparts));
//                l.Add(s);
//            }

//            return l;

//        }

//        public void Seek(long offset, System.IO.SeekOrigin origin)
//        {
//            this.offset = (int)offset;
//        }

//        public void Dispose()
//        {
//        }
//    }
//}
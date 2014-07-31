using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AzureStorageDrive.CopyJob;
using AzureStorageDrive.Util;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
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
    public class AwsMetaData
    {
        public int Size { get; set; }
    }
    public class AwsS3ServiceDriveInfo : AbstractDriveInfo
    {
        public IAmazonS3 Client { get; set; }
        public string AccountName { get; set; }
        public string Name { get; set; }

        public AwsS3ServiceDriveInfo(string url, string name)
        {
            var dict = ParseValues(url);
            var accountName = dict["account"];
            var accountKey = dict["key"];
            var region = dict["region"];
            var client = Amazon.AWSClientFactory.CreateAmazonS3Client(accountName, accountKey, RegionEndpoint.USEast1);
            this.Client = client;
            this.AccountName = accountName;
            this.Name = name;
        }

        public override void NewItem(
                            string path,
                            string type,
                            object newItemValue)
        {
            if (string.Equals(type, "Directory", StringComparison.InvariantCultureIgnoreCase))
            {
                this.CreateDirectory(path);
            }
            else if (string.Equals(type, "EmptyFile", StringComparison.InvariantCultureIgnoreCase))
            {
                if (newItemValue != null)
                {
                    var size = 0L;
                    if (long.TryParse(newItemValue.ToString(), out size))
                    {
                        this.CreateEmptyFile(path, size);
                    }
                    else
                    {
                        this.CreateEmptyFile(path, 0);
                    }
                }
            }
            else
            {
                var parts = PathResolver.SplitPath(path);
                if (parts.Count == 1)
                {
                    this.CreateContainer(parts[0]);
                }
                else
                {
                    this.CreateBlob(path, newItemValue.ToString());
                }
            }
        }
        public AwsMetaData GetMetaData(string bucketName, string key)
        {
            GetObjectMetadataRequest request = new GetObjectMetadataRequest{
                 BucketName = bucketName,
                 Key = key
            };
            var response = Client.GetObjectMetadata(request);
            return new AwsMetaData
            {
                Size = (int)response.ContentLength
            };
        }
        public override void GetChildItems(string path, bool recurse)
        {
            var items = this.ListItems(path);
            if (items != null)
            {
                foreach (var item in items)
                {
                    var s3O = item as S3Object;
                    if (s3O != null)
                    {
                        var r = AwsS3PathResolver.ResolvePath(Client, s3O.Key, false);
                        var isContainer = r.PathType == PathType.AwsS3Directory || r.PathType == PathType.AwsS3Root;
                        RootProvider.WriteItemObject(s3O, r.Key, isContainer);
                        if (recurse && isContainer)
                        {
                            GetChildItems(r.Key, recurse);
                        }
                    }
                    else
                    {
                        var s3B = item as S3Bucket;
                        RootProvider.WriteItemObject(s3B, "/", true);
                        if (recurse)
                        {
                            GetChildItems(s3B.BucketName, recurse);
                        }
                    }
                }
            }
        }

        public override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            var r = AwsS3PathResolver.ResolvePath(this.Client, path);
            switch (r.PathType)
            {
                case PathType.AwsS3Root:
                    Client.ListBuckets().Buckets.ForEach(b =>
                        {
                            RootProvider.WriteItemObject(b.BucketName,"",true);
                        });
                    break;
                case PathType.AwsS3Directory:
                    AwsS3Util.ListAndHandle(Client, r.BucketName, r.Prefix, o =>
                        {
                            //var oPath = AwsS3PathResolver.ResolvePath(Client,o.Key,false);
                            //RootProvider.WriteItemObject(oPath.Name, oPath.Prefix, oPath.PathType != PathType.AwsS3File);
                            RootProvider.WriteItemObject(o.Key, o.Key, o.Key.EndsWith(PathResolver.AlternatePathSeparator));
                            return true;
                        });
                    break;
                default:
                    break;
            }
        }

        public override void RemoveItem(string path, bool recurse)
        {
            var result = AwsS3PathResolver.ResolvePath(Client, path);
            switch (result.PathType)
            {
                case PathType.AwsS3File:
                    DeleteObjectRequest request = new DeleteObjectRequest
                    {
                        BucketName = result.BucketName,
                        Key = result.Key
                    };
                    Client.DeleteObjectAsync(request);
                    break;
                case PathType.AwsS3Directory:
                case PathType.AwsS3Root:
                    if (recurse)
                    {
                        DeleteObjectsRequest dlRequest = new DeleteObjectsRequest{
                             BucketName = result.BucketName,
                             Objects = new List<KeyVersion>()
                        };
                        ListObjectsRequest lRequest = new ListObjectsRequest
                        {
                            BucketName = result.BucketName,
                            Prefix = result.Key
                        };
                        var response = Client.ListObjects(lRequest);
                        response.S3Objects.ForEach(o =>
                            {
                                dlRequest.Objects.Add(new KeyVersion
                                {
                                    Key = o.Key
                                });
                            });
                        Client.DeleteObjects(dlRequest);
                    }
                    break;
            }
        }

        internal IEnumerable<object> ListItems(string path)
        {
            var result = AwsS3PathResolver.ResolvePath(Client, path,true,PathType.Unknown,false);
            switch (result.PathType)
            {
                case PathType.AwsS3File:
                    if (result.AlreadyExit)
                    {
                        return new List<S3Object>
                        {
                            new S3Object{
                                Key = result.Key,
                            }
                        };
                    }
                    break;
                case PathType.AwsS3Directory:
                    if (result.AlreadyExit)
                    {
                        ListObjectsRequest lRequest = new ListObjectsRequest
                        {
                            BucketName = result.BucketName,
                            Prefix = result.Key
                        };
                        var response = Client.ListObjects(lRequest);
                        return response.S3Objects.Where(s=>s.Key !=result.Key).ToList();
                    }
                    break;
                case PathType.AwsS3Root:
                    var lbResponse = Client.ListBuckets();
                    return lbResponse.Buckets;
            }
            return null;
        }

        private IEnumerable<object> ListContainers(CloudBlobClient client)
        {
            return client.ListContainers();
        }

        public bool IsDirEmpty(CloudBlobDirectory dir)
        {
            var r = dir.ListBlobsSegmented(true, BlobListingDetails.None, 1, null, null, null);
            return r.Results.Count() == 0;
        }

        public void ListAndHandle(CloudBlobDirectory dir,
            bool flatBlobListing = false,
            Action<ICloudBlob> blobAction = null,
            Action<CloudBlobDirectory> dirAction = null,
            Action<CloudBlobContainer> containerAction = null)
        {
            BlobContinuationToken token = null;
            while (true)
            {
                var r = dir.ListBlobsSegmented(flatBlobListing, BlobListingDetails.None, 10, token, null, null);
                token = r.ContinuationToken;
                var blobs = r.Results;
                this.HandleItems(blobs, blobAction, dirAction, containerAction);

                if (token == null)
                {
                    break;
                }
            }
        }

        public void HandleItems(IEnumerable<object> items,
            Action<ICloudBlob> blobAction = null,
            Action<CloudBlobDirectory> dirAction = null,
            Action<CloudBlobContainer> containerAction = null)
        {
            if (items == null)
            {
                return;
            }

            foreach (var i in items)
            {
                var d = i as CloudBlobDirectory;
                if (d != null)
                {
                    dirAction(d);
                    continue;
                }

                var f = i as ICloudBlob;
                if (f != null)
                {
                    blobAction(f);
                    continue;
                }

                var s = i as CloudBlobContainer;
                if (s != null)
                {
                    containerAction(s);
                    continue;
                }
            }
        }

        private IEnumerable<IListBlobItem> ListBlob(ICloudBlob file)
        {
            return new IListBlobItem[] { file };
        }

        private IEnumerable<object> ListDirectory(CloudBlobDirectory dir)
        {
            var list = new List<object>();
            ListAndHandle(dir,
                blobAction: b => list.Add(b),
                dirAction: d => list.Add(d));
            return list;
        }

        internal void CreateDirectory(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var result = AwsS3PathResolver.ResolvePath(Client, path,true, PathType.AwsS3Directory);
                if (!result.AlreadyExit)
                {
                    PutObjectRequest request = new PutObjectRequest
                    {
                        BucketName = result.BucketName,
                        Key = result.Key
                    };

                    Client.PutObject(request);
                }
            }
        }

        internal void CreateContainer(CloudBlobDirectory dir)
        {
            var container = dir.Container;
            if (!container.Exists())
            {
                container.Create();
            }
        }

        internal void CreateEmptyFile(string path, long size)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var result = AwsS3PathResolver.ResolvePath(Client, path,true, PathType.AwsS3Directory);
                if (!result.AlreadyExit)
                {
                    PutObjectRequest request = new PutObjectRequest
                    {
                        BucketName = result.BucketName,
                        Key = result.Key,
                    };
                    Client.PutObject(request);
                }
            }
        }

        internal void CreateBlob(string path, string content)
        {
            var file = GetBlob(path, PathType.AzureBlobBlock) as CloudBlockBlob;
            if (file == null)
            {
                throw new Exception("Path " + path + " is not a valid file path.");
            }

            CreateContainer(file.Parent);
            file.UploadText(content);
        }

        public override IContentReader GetContentReader(string path)
        {
            throw new NotImplementedException();

            //var r = AzureBlobPathResolver.ResolvePath(this.Client, path, hint: PathType.AzureBlobBlock, skipCheckExistence: false);
            //if (r.PathType == PathType.AzureBlobBlock)
            //{
            //    var reader = new AzureBlobReader(GetBlob(path, PathType.Unknown));
            //    return reader;
            //}

            //return null;
        }

        public ICloudBlob GetBlob(string path, PathType expectedType)
        {
            throw new NotImplementedException();

            //var r = AzureBlobPathResolver.ResolvePath(this.Client, path, hint: expectedType);
            //if (r.PathType == expectedType)
            //{
            //    return r.Blob;
            //}

            //return null;
        }

        internal void DeleteDirectory(CloudBlobDirectory dir, bool recurse)
        {
            if (recurse)
            {
                ListAndHandle(dir,
                    flatBlobListing: true,
                    blobAction: (b) => b.Delete());
            }
            else
            {
                if (!IsDirEmpty(dir))
                {
                    throw new Exception("The directory is not empty. Please specify -recurse to delete it.");
                }
            }
        }

        internal CloudBlobContainer CreateContainer(string containerName)
        {
            throw new NotImplementedException();

            //var container = this.Client.GetContainerReference(containerName);
            //if (!container.Exists())
            //{
            //    container.Create();
            //    return container;
            //}

            //throw new Exception("Container " + containerName + " already exists");
        }

        internal void Download(string path, string destination)
        {
            var result = AwsS3PathResolver.ResolvePath(Client, path);
            if(result.AlreadyExit)
            {
                var response = Client.GetObject(new GetObjectRequest
                {
                    BucketName = result.BucketName,
                    Key = result.Key
                });
                response.WriteResponseStreamToFile(destination, false);
            }
        }

        private void DownloadContainer(CloudBlobContainer container, string destination)
        {
            destination = PathResolver.Combine(destination, container.Name);
            Directory.CreateDirectory(destination);

            var dir = container.GetDirectoryReference("");
            ListAndHandle(dir,
                flatBlobListing: false,
                blobAction: (b) => b.DownloadToFile(PathResolver.Combine(destination, b.Name), FileMode.CreateNew),
                dirAction: (d) => DownloadDirectory(d, destination));
        }

        internal void DownloadDirectory(CloudBlobDirectory dir, string destination)
        {
            destination = Path.Combine(destination, dir.Prefix);
            Directory.CreateDirectory(destination);

            ListAndHandle(dir,
                flatBlobListing: false,
                blobAction: (b) => b.DownloadToFile(PathResolver.Combine(destination, b.Name), FileMode.CreateNew),
                dirAction: (d) => DownloadDirectory(d, destination));
        }

        internal void Upload(string localPath, string targePath)
        {
            var result = AwsS3PathResolver.ResolvePath(Client, targePath);
            if (result.AlreadyExit)
            {
                Client.DeleteObject(new DeleteObjectRequest
                {
                    BucketName = result.BucketName,
                    Key = result.Key
                });
            };
            Client.PutObject(new PutObjectRequest
            {
                BucketName = result.BucketName,
                FilePath = localPath,
                Key = result.Key
            });
        }

        private void UploadDirectory(string localPath, CloudBlobDirectory dir)
        {
            var localDirName = Path.GetFileName(localPath);
            var subdir = dir.GetDirectoryReference(localDirName);

            foreach (var f in Directory.GetFiles(localPath))
            {
                UploadFile(f, subdir);
            }

            foreach (var d in Directory.GetDirectories(localPath))
            {
                UploadDirectory(d, subdir);
            }
        }

        private void UploadFile(string localFile, CloudBlobDirectory dir)
        {
            var file = Path.GetFileName(localFile);
            var f = dir.GetBlockBlobReference(file);
            var condition = new AccessCondition();
            f.UploadFromFile(localFile, FileMode.CreateNew);
        }

        public override bool HasChildItems(string path)
        {
            var result = AwsS3PathResolver.ResolvePath(Client, path);
            switch(result.PathType)
            {
                case PathType.AwsS3Root:
                    return Client.ListBuckets().Buckets.Count > 0;
                case PathType.AwsS3Directory:
                    ListObjectsRequest request = new ListObjectsRequest
                    {
                        BucketName = result.BucketName,
                        Prefix = result.Key
                    };
                    return Client.ListObjects(request).S3Objects.Count > 0;
                default:
                    break;
            }
            return false;
        }

        public override bool IsValidPath(string path)
        {
            return true;
        }

        public override bool ItemExists(string path)
        {
            return AwsS3PathResolver.ResolvePath(Client, path,true,PathType.Unknown,false).AlreadyExit;
        }

        public override bool IsItemContainer(string path)
        {
            var result = AwsS3PathResolver.ResolvePath(Client, path);
            return result.PathType == PathType.AwsS3Directory || result.PathType == PathType.AwsS3Root;
        }

        public override void GetProperty(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList)
        {
            throw new NotImplementedException();

            //var r = AzureBlobPathResolver.ResolvePath(this.Client, path, skipCheckExistence: false);
            //switch (r.PathType)
            //{
            //    case PathType.AzureBlobBlock:
            //        r.Blob.FetchAttributes();
            //        this.RootProvider.WriteItemObject(r.Blob.Properties, path, false);
            //        this.RootProvider.WriteItemObject(r.Blob.Metadata, path, false);
            //        break;
            //    case PathType.AzureBlobDirectory:
            //        if (r.Directory == r.RootDirectory)
            //        {
            //            r.Container.FetchAttributes();
            //            this.RootProvider.WriteItemObject(r.Container.Properties, path, true);
            //            this.RootProvider.WriteItemObject(r.Container.Metadata, path, true);
            //        }
            //        else
            //        {
            //            //none to show
            //        }
            //        break;
            //    default:
            //        break;
            //}
        }

        public override void SetProperty(string path, PSObject propertyValue)
        {
            throw new NotImplementedException();

            //var r = AzureBlobPathResolver.ResolvePath(this.Client, path, skipCheckExistence: false);
            //switch (r.PathType)
            //{
            //    case PathType.AzureBlobBlock:
            //        r.Blob.FetchAttributes();
            //        MergeProperties(r.Blob.Metadata, propertyValue.Properties);
            //        r.Blob.SetMetadata();
            //        break;
            //    case PathType.AzureBlobDirectory:
            //        if (r.Parts.Count() == 1)
            //        {
            //            r.Container.FetchAttributes();
            //            MergeProperties(r.Container.Metadata, propertyValue.Properties);
            //            r.Container.SetMetadata();
            //        }
            //        else
            //        {
            //            throw new Exception("Setting metadata/properties for directory is not supported");
            //        }
            //        break;
            //    default:
            //        break;
            //}
        }

        private void MergeProperties(IDictionary<string, string> target, PSMemberInfoCollection<PSPropertyInfo> source)
        {
            foreach (var info in source)
            {
                var name = info.Name;
                if (target.ContainsKey(name))
                {
                    target.Remove(name);
                }

                target.Add(name, info.Value.ToString());
            }
        }

        #region Data copy
        public bool CopyItem(string localPath, AzureBlobServiceDriveInfo targetDrive, string targetPath, bool recurse)
        {
            return false;
        }

        public bool CopyItem(AzureBlobServiceDriveInfo drive, string source, string localPath, bool recurse)
        {
            return false;
        }

        public bool CopyItem(AzureBlobServiceDriveInfo sourceDrive, string sourcePath, AzureFileServiceDriveInfo targetDrive, string targetPath, bool recurse)
        {
            return false;
        }
        #endregion

        public override CopyJob.ICopySource GetCopySource(string path)
        {
            return new AwsS3CopySource(this, path);
        }

        public override CopyJob.ICopyTarget GetCopyTarget(string path)
        {
            return new AwsS3CopyTarget(this, path);
        }
    }

    class AwsS3Reader : IContentReader
    {
        private ICloudBlob File { get; set; }
        private long length = 0;
        private long offset = 0;
        private const int unit = 80;
        public AwsS3Reader(ICloudBlob file)
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
            var fullparts = (int)Math.Floor(total * 1.0 / unit);
            for (var i = 0; i < fullparts; ++i)
            {
                var s = Encoding.UTF8.GetString(b, i * unit, unit);

                l.Add(s);
            }

            //last part
            if (total > unit * fullparts)
            {
                var s = Encoding.UTF8.GetString(b, fullparts * unit, (int)(total - unit * fullparts));
                l.Add(s);
            }

            return l;

        }

        public void Seek(long offset, System.IO.SeekOrigin origin)
        {
            this.offset = (int)offset;
        }

        public void Dispose()
        {
        }
    }
}

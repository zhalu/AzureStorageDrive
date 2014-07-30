using Amazon.S3.Model;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using System.Collections.Generic;
using System.Linq;

namespace AzureStorageDrive
{
    public class AwsS3PathResolveResult
    {
        public AwsS3PathResolveResult()
        {
            this.PathType = PathType.Invalid;
        }
        public string BucketName { get; set; }
        public PathType PathType { get; set; }
        public string Name { get; set; }
        public string Prefix { get; set; }
        public string Key { get; set; }
        public bool IsRootDirectory { get; set; }
        public bool AlreadyExit { get; set; }
    }
}

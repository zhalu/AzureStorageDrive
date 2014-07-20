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
        public PathType PathType { get; set; }
        public string BucketName { get; set; }
        public string DirectoryName { get; set; }
        public S3Object Object { get; set; }
        public bool IsRootDirectory { get; set; }
        public List<string> Parts { get; set; }
    }
}

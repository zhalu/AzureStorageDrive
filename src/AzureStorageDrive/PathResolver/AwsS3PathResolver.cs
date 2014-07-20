//using Amazon.S3;
//using Amazon.S3.Model;
//using AzureStorageDrive.Util;
//using Microsoft.WindowsAzure.Storage.Blob;
//using Microsoft.WindowsAzure.Storage.File;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;

//namespace AzureStorageDrive
//{
//    public class AwsS3PathResolver : PathResolver
//    {
//        public static AwsS3PathResolveResult ResolvePath(IAmazonS3 client, string path, PathType hint = PathType.Unknown, bool skipCheckExistence = true)
//        {
//            var result = new AwsS3PathResolveResult();
//            var parts = SplitPath(path);
//            if (!ValidatePath(parts))
//            {
//                throw new Exception("Path " + path + " is invalid");
//            }

//            result.Parts = parts;

//            if (parts.Count == 0)
//            {
//                result.PathType = PathType.AzureBlobRoot;
//            }

//            if (parts.Count > 0)
//            {
//                result.BucketName = parts[0];
//                result.DirectoryName = string.Empty;
//                result.PathType = PathType.AwsS3Directory;
//                result.IsRootDirectory = true;

//                if (!skipCheckExistence && !AwsS3Util.CheckBucketExistence(client, result.BucketName))
//                {
//                    result.PathType = PathType.Invalid;
//                    return result;
//                }
//            }

//            if (parts.Count > 1)
//            {
//                result.IsRootDirectory = false;

//                if (hint == PathType.AwsS3Directory || hint == PathType.Unknown)
//                {
//                    //assume it's directory first
//                    var dir = result.Directory.GetDirectoryReference(parts.Last());
//                    if (result.PathType == PathType.AzureBlobDirectory 
//                        && (skipCheckExistence || dir.ListBlobsSegmented(false, BlobListingDetails.None, 1, null, null, null).Results.Count() > 0))
//                    {
//                        result.Directory = dir;
//                        result.PathType = PathType.AzureBlobDirectory;
//                        return result;
//                    }

//                }

//                //2. assume it's an object
//                if (hint == PathType.AzureBlobBlock || hint == PathType.Unknown)
//                {
//                    var blob = result.Directory.GetBlockBlobReference(parts.Last());
//                    if (result.PathType == PathType.AzureBlobDirectory && (skipCheckExistence || blob != null))
//                    {
//                        result.Blob = blob as ICloudBlob;
//                        result.PathType = PathType.AzureBlobBlock;
//                        return result;
//                    }
//                }

//                result.PathType = PathType.Unknown;
//            }

//            if (result.PathType == PathType.AzureBlobDirectory && hint == PathType.AzureBlobBlock)
//            {
//                result.PathType = PathType.Invalid;
//            }

//            return result;
//        }

//        public static bool ValidatePath(List<string> parts)
//        {
//            if (parts.Count == 0)
//            {
//                return true;
//            }

//            //todo: add more checks here
//            return true;
//        }
//    }
//}

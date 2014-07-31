using Amazon.S3;
using Amazon.S3.Model;
using AzureStorageDrive.Util;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AzureStorageDrive
{
    public class AwsS3PathResolver : PathResolver
    {
        private static void ResolvePath(AwsS3PathResolveResult result, IAmazonS3 client, string path, PathType hint = PathType.Unknown, bool skipCheckExistence = true)
        {
            var parts = SplitPath(path);
            if (parts.Count == 0)
            {
                return;
            }
            result.PathType = PathType.AwsS3Directory;
            result.IsRootDirectory = false;
            result.Key = string.Join(AlternatePathSeparator, parts);
            result.Name = parts.Last();
            result.Prefix = string.Join(AlternatePathSeparator, parts.Take(parts.Count-1));
            bool isFile = false;
            if (!skipCheckExistence && AwsS3Util.CheckFileExistence(client, result.BucketName, result.Key,hint, out isFile))
            {
                result.AlreadyExit = true;
            }
            else
            {
                result.AlreadyExit = false;
            }            
            if(isFile)
            {
                result.PathType = PathType.AwsS3File;
            }
            else
            {
                if(!result.Key.EndsWith(AlternatePathSeparator))
                {
                    result.Key += AlternatePathSeparator;
                }
            }
        }
        public static AwsS3PathResolveResult ResolvePath(IAmazonS3 client, string path, bool includeBucket = true, PathType hint = PathType.Unknown, bool skipCheckExistence = true)
        {
            var result = new AwsS3PathResolveResult();
            
            var parts = SplitPath(path);

            if (!ValidatePath(parts))
            {
                throw new Exception("Path " + path + " is invalid");
            }
            if (parts.Count == 0)
            {
                if (includeBucket)
                {
                    result.PathType = PathType.AwsS3Root;
                    result.AlreadyExit = true;
                }
                else
                {
                    result.PathType = PathType.AwsS3Directory;
                    result.IsRootDirectory = true;
                }
            }
            else
            {
                result.PathType = PathType.AwsS3Directory;
                result.IsRootDirectory = true;
                if (includeBucket)
                {
                    if (!skipCheckExistence && !AwsS3Util.CheckBucketExistence(client, parts[0]))
                    {
                        result.PathType = PathType.Invalid;
                    }
                    else
                    {
                        result.BucketName = parts[0];
                        result.AlreadyExit = true;
                        if (parts.Count > 1)
                        {
                            ResolvePath(result, client, AwsS3PathResolver.ConverToAwsS3Path(AwsS3PathResolver.Combine(parts.Skip(1))), hint, skipCheckExistence);
                        }
                    }
                }
                else
                {
                    ResolvePath(result, client, AwsS3PathResolver.ConverToAwsS3Path(path), hint, skipCheckExistence);
                }
            }
            return result;
        }

        public static string ConverToAwsS3Path(string path)
        {
            return path.Replace('\\', '/');
        }

        public static bool ValidatePath(List<string> parts)
        {
            //if (parts.Count == 0)
            //{
            //    return true;
            //}

            //if (!Regex.Match(parts[0], SharePattern).Success)
            //{
            //    return false;
            //}

            //for (var i = 1; i < parts.Count; ++i)
            //{
            //    if (!Regex.Match(parts[i], FilePattern).Success)
            //    {
            //        return false;
            //    }
            //}

            return true;
        }
    }
}

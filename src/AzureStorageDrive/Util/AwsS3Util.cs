using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.Util
{
    public static class AwsS3Util
    {
        public static bool CheckBucketExistence(IAmazonS3 client, string bucketName)
        {
            var r = new GetBucketLocationRequest();
            r.BucketName = bucketName;
            try
            {
                var resp = client.GetBucketLocation(r);
                return resp.HttpStatusCode == HttpStatusCode.OK;
            }
            catch { }
            return false;
        }

        public static bool CheckFileExistence(IAmazonS3 client, string bucketName, string key,out bool isFile)
        {
            try
            {
                //Check File first
                isFile = true;
                GetObjectRequest request = new GetObjectRequest { BucketName = bucketName, Key = key };
                using (GetObjectResponse response = client.GetObject(request))
                {
                    return true;
                }
            }
            catch 
            {
                //Not a file, check directory
                isFile = false;
                ListObjectsRequest request = new ListObjectsRequest { BucketName = bucketName, Prefix = key + PathResolver.AlternatePathSeparator };
                try
                {
                    client.ListObjects(request);
                    return true;
                }
                catch { }
            }
            //catch (AmazonS3Exception amazonS3Exception)
            //{
            //    if (amazonS3Exception.ErrorCode != null &&
            //        (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
            //        amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
            //    {
            //        Console.WriteLine("Please check the provided AWS Credentials.");
            //        Console.WriteLine("If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
            //    }
            //    else
            //    {
            //        Console.WriteLine("An error occurred with the message '{0}' when reading an object", amazonS3Exception.Message);
            //    }
            //}
            return false;
        }
        public static void ListAndHandle(IAmazonS3 client, string bucketName, string prefix, Func<S3Object, bool> handler)
        {
            ListObjectsRequest request = new ListObjectsRequest
            {
                BucketName = bucketName,
                Prefix = prefix
            };
            try
            {
                ListObjectsResponse response = client.ListObjects(request);
                foreach (var item in response.S3Objects)
                {
                    handler(item);
                }
            }
            catch { }
            //catch (AmazonS3Exception amazonS3Exception)
            //{
            //    if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
            //    {
            //        Console.WriteLine("Please check the provided AWS Credentials.");
            //        Console.WriteLine("If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
            //    }
            //    else
            //    {
            //        Console.WriteLine("An error occurred with the message '{0}' when listing objects", amazonS3Exception.Message);
            //    }
            //}
        } 
    }
}

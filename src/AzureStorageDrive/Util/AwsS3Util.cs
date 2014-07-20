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
            var resp = client.GetBucketLocation(r);
            return resp.HttpStatusCode == HttpStatusCode.OK;
        }

        public static void ListAndHandle(IAmazonS3 client, string bucketName, string prefix, Func<S3Object, bool> handler)
        {

        } 
    }
}

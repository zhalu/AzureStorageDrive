using Amazon.S3.Model;
using AzureStorageDrive.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public class AwsS3CopyTarget : ICopyTarget
    {
        AwsS3ServiceDriveInfo Drive { get; set; }
        public string Target { get; set; }

        public AwsS3PathResolveResult Result { get; set; }

        public AwsS3CopyTarget(AwsS3ServiceDriveInfo drive, string target)
        {
            Drive = drive;
            Target = target;
            Result = AwsS3PathResolver.ResolvePath(Drive.Client, Target, skipCheckExistence: false);
        }
        public bool Prepare(string name, long size)
        {
            if(size > Constants.TB)
            {
                return false;
            }
            if(!Result.AlreadyExit)
            {
                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = Result.BucketName,
                    Key = Result.Key
                };
                Drive.Client.PutObject(request);
                return true;
            }
            return false;
        }

        public void Go(byte[] bytes, int offset, int count, long targetOffset, int blockId)
        {
            UploadPartRequest request = new UploadPartRequest
            {
                BucketName = Result.BucketName,
                Key = Result.Key,
                InputStream = new MemoryStream(bytes, offset, count) { Position = 0 },
                PartSize = count,
                IsLastPart = count < Constants.BlockSize
            };
            Drive.Client.UploadPart(request);
        }

        public void Done(int blockCount)
        {
            //throw new NotImplementedException();
        }
    }
}

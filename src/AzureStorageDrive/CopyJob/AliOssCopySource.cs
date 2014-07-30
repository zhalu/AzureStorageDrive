using Aliyun.OpenServices.OpenStorageService;
using AzureStorageDrive.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public class AliOssCopySource : ICopySource
    {
        public AliOssServiceDriveInfo Drive { get; private set;}
        public string SourcePath { get; private set; }

        public AliOssCopySource(AliOssServiceDriveInfo drive, string path)
        {
            this.Drive = drive;
            this.SourcePath = path;
        }

        public void CopyTo(ICopyTarget target, bool recurse, bool deleteSource)
        {
            var result = AliOssPathResolver.ResolvePath(this.SourcePath);
            if (result.PathType == AliOssPathType.Object)
            {
                ObjectMetadata meta = Drive.GetObjectMetaData(result.Bucket, result.Prefix);

                var length = meta.ContentLength;
                var name = result.Name;

                if (target.Prepare(name, length))
                {
                    var buffer = new byte[Constants.BlockSize * Constants.Parallalism];
                    var blockCount = (int)Math.Ceiling(length / (1.0 * Constants.BlockSize));

                    System.Threading.Tasks.Parallel.For(0, Constants.Parallalism, (i) =>
                    {
                        GetObjectRequest gtObjRequest = new GetObjectRequest(result.Bucket, result.Prefix);

                        var iteration = 0;
                        while (true)
                        {
                            var start = iteration * (buffer.Length) + i * Constants.BlockSize;
                            var count = Constants.BlockSize;

                            //if we already pass the end, then we are done.
                            if (start >= length)
                            {
                                break;
                            }

                            //if it's the last block, change the end
                            if (length < start + count)
                            {
                                count = (int)(length - start);
                            }

                            try
                            {
                                //read the part
                                //result.File.DownloadRangeToByteArray(buffer, i * Constants.BlockSize, start, count);
                                
                                gtObjRequest.SetRange(start, start + count - 1);
                                OssObject obj = this.Drive.Client.GetObject(gtObjRequest);
                                int offset = 0;
                                int bytesRead = 0;
                                while ((bytesRead = obj.Content.Read(buffer, i * Constants.BlockSize + offset, Constants.BlockSize)) > 0)
                                {
                                    offset += bytesRead;
                                }

                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }

                            //put it
                            target.Go(buffer, i * Constants.BlockSize, count, start, i + iteration * Constants.Parallalism);

                            iteration++;
                        }
                    });

                    target.Done(blockCount);
                }
            }
        }
    }
}

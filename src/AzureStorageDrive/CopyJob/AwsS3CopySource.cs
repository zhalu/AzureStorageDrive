using Amazon.S3.Model;
using AzureStorageDrive.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public class AwsS3CopySource : ICopySource
    {
        public AwsS3ServiceDriveInfo Drive { get; set; }
        public string Source { get; set; }
        public AwsS3CopySource(AwsS3ServiceDriveInfo drive, string source)
        {
            Drive = drive;
            Source = source;
        }
        public void CopyTo(ICopyTarget target, bool recurse, bool deleteSource)
        {
            var result = AwsS3PathResolver.ResolvePath(Drive.Client, Source, true, PathType.Unknown, false);
            if (result.AlreadyExit)
            {
                switch (result.PathType)
                {
                    case PathType.AwsS3Root:
                        break;
                    case PathType.AwsS3Directory:
                        break;
                    case PathType.AwsS3File:
                        var metaData = Drive.GetMetaData(result.BucketName, result.Key);
                        if(target.Prepare(result.Name,metaData.Size))
                        {
                            var buffer = new byte[Constants.BlockSize * Constants.Parallalism];
                            var blockCount = (int)Math.Ceiling(metaData.Size / (1.0 * Constants.BlockSize));

                            System.Threading.Tasks.Parallel.For(0, Constants.Parallalism, (i) =>
                            {
                                GetObjectRequest gtObjRequest = new GetObjectRequest
                                {
                                    BucketName = result.BucketName,
                                    Key = result.Key,
                                };

                                var iteration = 0;
                                while (true)
                                {
                                    var start = iteration * (buffer.Length) + i * Constants.BlockSize;
                                    var end = start + Constants.BlockSize;

                                    //if we already pass the end, then we are done.
                                    if (start >= metaData.Size)
                                    {
                                        break;
                                    }

                                    //if it's the last block, change the end
                                    if (metaData.Size < end)
                                    {
                                        end = metaData.Size;
                                    }
                                    int count = end - start;
                                    try
                                    {
                                        //read the part
                                        //result.File.DownloadRangeToByteArray(buffer, i * Constants.BlockSize, start, count);

                                        gtObjRequest.ByteRange = new ByteRange(start, end);
                                        var obj = this.Drive.Client.GetObject(gtObjRequest);
                                        int offset = 0;
                                        int bytesRead = 0;
                                        while (count > bytesRead)
                                        {
                                            var r = obj.ResponseStream.Read(buffer, i * Constants.BlockSize + offset, end - start - bytesRead);
                                            offset += r;
                                            bytesRead += r;
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
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

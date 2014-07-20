using AzureStorageDrive.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public class AzureBlobCopySource : ICopySource
    {
        public AzureBlobServiceDriveInfo Drive { get; private set; }

        public string SourcePath {get;private set;}

        public AzureBlobCopySource(AzureBlobServiceDriveInfo drive, string path)
        {
            this.Drive = drive;
            this.SourcePath = path;
        }

        public void CopyTo(ICopyTarget target, bool recurse, bool deleteSource)
        {
            var r = AzureBlobPathResolver.ResolvePath(this.Drive.Client, this.SourcePath, skipCheckExistence: false);
            if (r.PathType == PathType.AzureBlobBlock || r.PathType == PathType.AzureBlobPage)
            {
                r.Blob.FetchAttributes();
                var length = r.Blob.Properties.Length;
                var name = r.Blob.Name;

                if (target.Prepare(name, length))
                {
                    var buffer = new byte[Constants.BlockSize * Constants.Parallalism];
                    var blockCount = (int)Math.Ceiling(length / (1.0 * Constants.BlockSize));

                    System.Threading.Tasks.Parallel.For(0, Constants.Parallalism, (i) =>
                        {
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
                                    r.Blob.DownloadRangeToByteArray(buffer, i * Constants.BlockSize, start, count);
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
            
            //TODO: need to check file directory
        }
    }
}

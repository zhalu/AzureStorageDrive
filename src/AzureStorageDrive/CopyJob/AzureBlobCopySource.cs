using AzureStorageDrive.Util;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            switch (r.PathType)
            {
                case PathType.AzureBlobRoot:
                    CopyRoot(r, target, recurse, deleteSource);
                    break;
                case PathType.AzureBlobDirectory:
                    CopyDirectory(r.Directory, target, recurse, deleteSource);
                    break;
                case PathType.AzureBlobPage:
                case PathType.AzureBlobBlock:
                    CopyBlob(r.Blob, target, recurse, deleteSource);
                    break;
                default:
                    break;
            }
        }

        private void CopyRoot(AzureBlobPathResolveResult r, ICopyTarget target, bool recurse, bool deleteSource)
        {
            throw new NotImplementedException();
        }

        private void CopyDirectory(CloudBlobDirectory cloudBlobDirectory, ICopyTarget target, bool recurse, bool deleteSource)
        {
            throw new NotImplementedException();
        }

        private void CopyBlob(ICloudBlob blob, ICopyTarget target, bool recurse, bool deleteSource)
        {
            blob.FetchAttributes();
            var length = blob.Properties.Length;
            var name = blob.Name;

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

                        //retry when errors
                        while (true)
                        {
                            try
                            {
                                //read the part
                                blob.DownloadRangeToByteArray(buffer, i * Constants.BlockSize, start, count);
                                break;
                            }
                            catch (Exception)
                            {
                                Thread.Sleep(100);
                            }
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

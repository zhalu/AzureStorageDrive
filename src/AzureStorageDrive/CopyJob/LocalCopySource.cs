using AzureStorageDrive.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public class LocalCopySource : ICopySource
    {
        public string LocalPath { get; set; }

        public void CopyTo(ICopyTarget target, bool recurse, bool deleteSource)
        {
            if (File.Exists(this.LocalPath))
            {
                var file = new FileInfo(this.LocalPath);
                var length = file.Length;
                var name = file.Name;
                if (target.Prepare(name, length))
                {
                    var buffer = new byte[Constants.BlockSize * Constants.Parallalism];
                    var blockCount = (int) Math.Ceiling(length / (1.0 * Constants.BlockSize));

                    System.Threading.Tasks.Parallel.For(0, Constants.Parallalism, (i) =>
                        {
                            var iteration = 0;
                            var s = new FileStream(this.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
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

                                //read the part
                                s.Seek(start, SeekOrigin.Begin);
                                s.Read(buffer, i * Constants.BlockSize, count);

                                //put it
                                target.Go(buffer, i * Constants.BlockSize, count, start, i + iteration * Constants.Parallalism);

                                iteration++;
                            }

                            s.Close();
                            s.Dispose();
                            s = null;
                        });

                    target.Done(blockCount);
                }
            }
            else if (Directory.Exists(this.LocalPath))
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new Exception("Path " + this.LocalPath + " is not recognized as local path");
            }
        }
    }
}

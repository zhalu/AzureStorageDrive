using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public class LocalCopyTarget : ICopyTarget
    {
        public string LocalPath { get; private set; }
        private string Name;

        public LocalCopyTarget(string localPath)
        {
            this.LocalPath = localPath;
        }
        public bool Prepare(string name, long size)
        {
            this.Name = name;
            var path = JoinPath(this.LocalPath, name);
            
            using (var stream = File.Create(path))
            {
                try
                {
                    stream.SetLength(size);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            //TODO: need to support user specify the target file name
            return true;
        }

        private string JoinPath(string p1, string p2)
        {
            if (p1.EndsWith("\\") && p2.StartsWith("\\"))
            {
                return p1 + p2.Substring(1);
            }
            else if (p1.EndsWith("\\") && !p2.StartsWith("\\"))
            {
                return p1 + p2;
            }
            else if (!p1.EndsWith("\\") && p2.StartsWith("\\"))
            {
                return p1 + p2;
            }
            else
            {
                return p1 + "\\" + p2;
            }
        }

        public void Go(byte[] bytes, int offset, int count, long targetOffset, int blockId)
        {
            using (var stream = new FileStream(JoinPath(this.LocalPath, this.Name), FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                stream.Seek(targetOffset, SeekOrigin.Begin);
                stream.Write(bytes, offset, count);
            }
        }

        public void Done(int blockCount)
        {
            //nothing to do
        }
    }
}

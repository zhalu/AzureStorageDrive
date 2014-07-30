using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public class AliOssCopyTarget : ICopyTarget
    {

        public bool Prepare(string name, long size)
        {
            throw new NotImplementedException();
        }

        public void Go(byte[] bytes, int offset, int count, long targetOffset, int blockId)
        {
            throw new NotImplementedException();
        }

        public void Done(int blockCount)
        {
            throw new NotImplementedException();
        }
    }
}

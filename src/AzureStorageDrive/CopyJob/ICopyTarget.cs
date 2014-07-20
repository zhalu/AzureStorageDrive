using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public interface ICopyTarget
    {
        bool Prepare(string name, long size);

        void Go(byte[] bytes, int offset, int count, long targetOffset, int blockId);

        void Done(int blockCount);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public interface ICopyableSource
    {
        IEnumerable<CopySourceItem> ListFilesForCopy(string path, bool recurse, List<string> baseParts = null);

        bool IsRenameSupported();

        void DownloadRange(CopySourceItem copySourceItem, byte[] target, int index, long fileOffset, int length);
    }
}

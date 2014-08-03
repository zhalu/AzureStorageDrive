using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public class CopySourceItem
    {
        public string RelativePath { get; set; }
        public long Size { get; set; }
        public object Object { get; set; }
    }
}

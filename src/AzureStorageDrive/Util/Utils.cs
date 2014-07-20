using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.Util
{
    public static class Utils
    {

        public static IEnumerable<string> GetBlockIdArray(int length)
        {
            for (var i = 0; i < length; ++i)
            {
                yield return GetBlockId(i);
            }
        }

        public static string GetBlockId(int id)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0:d10}", id)));
        }
    }
}

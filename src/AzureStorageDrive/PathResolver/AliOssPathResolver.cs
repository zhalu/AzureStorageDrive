using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AzureStorageDrive
{
    public class AliOssPathResolver : PathResolver
    {
        public static AliOssPathResolveResult ResolvePath(string path)
        {
            var result = new AliOssPathResolveResult();
            var parts = SplitPath(path);
            if (!ValidatePath(parts))
            {
                throw new Exception("Path " + path + " is invalid");
            }

            if (parts.Count == 0)
            {
                result.PathType = AliOssPathType.Root;
            }
            else if (parts.Count == 1)
            {
                result.PathType = AliOssPathType.Bucket;
                result.Bucket = parts[0];
            }
            else
            {
                result.PathType = AliOssPathType.Object;
                result.Bucket = parts[0];
                parts.RemoveAt(0);
                result.Prefix = string.Join("/", parts);
                result.Name = parts[parts.Count - 1];
            }
            
            return result;
        }

        public static bool ValidatePath(List<string> parts)
        {
            /* TODO:
             * Validate path
             */
            return true;
        }
    }
}

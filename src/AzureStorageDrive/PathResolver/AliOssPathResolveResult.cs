using System.Collections.Generic;

namespace AzureStorageDrive
{
    public class AliOssPathResolveResult
    {
        public AliOssPathResolveResult()
        {
            PathType = AliOssPathType.Invalid;
        }
        public AliOssPathType PathType { get; set; }
        public string Bucket { get; set; }
        public string Prefix { get; set; }
        public string Name { get; set; }
    }

    public enum AliOssPathType
    {
        Invalid,
        Root,
        Bucket,
        Object
    }
}

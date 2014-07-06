using Microsoft.WindowsAzure.Storage.File;
using System.Collections.Generic;

namespace AzureStorageDrive
{
    public class PathResolveResult
    {
        public PathResolveResult()
        {
            this.PathType = AzureStorageDrive.PathType.Invalid;
        }
        public PathType PathType { get; set; }
        public CloudFileShare Share { get; set; }
        public CloudFileDirectory Directory { get; set; }
        public CloudFile File { get; set; }
        public CloudFileDirectory RootDirectory { get; set;}

        public List<string> Parts { get; set; }
        public bool Exists()
        {
            switch (PathType)
            {
                case AzureStorageDrive.PathType.File:
                    return this.File.Exists();
                case AzureStorageDrive.PathType.Directory:
                    return this.Directory.Exists();
                case AzureStorageDrive.PathType.Root:
                    return true;
                default:
                    return false;
            }
        }
    }
}

using AzureStorageDrive.Util;
using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public class AzureFileCopyTarget : ICopyTarget
    {
        public AzureFileServiceDriveInfo Drive { get; private set; }
        public string BasePath { get; private set; }
        public CloudFile CloudFile { get; private set; }

        public AzureFileCopyTarget(AzureFileServiceDriveInfo drive, string basePath)
        {
            this.Drive = drive;
            this.BasePath = basePath;
        }

        public bool Prepare(string name, long size)
        {
            if (size > Constants.TB)
            {
                return false;
            }

            var r = AzureFilePathResolver.ResolvePath(this.Drive.Client, this.BasePath, skipCheckExistence: false);
            if (r.PathType == PathType.AzureFileDirectory)
            {
                this.CloudFile = r.Directory.GetFileReference(name);
                try
                {
                    this.CloudFile.Create(size);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            //TODO: need to support user specify the target file name
            return false;
        }

        public void Go(byte[] bytes, int offset, int count, long targetOffset, int blockId)
        {
            using (var m = new MemoryStream(bytes, (int)offset, count))
            {
                this.CloudFile.WriteRange(m, targetOffset);
            }
        }

        public void Done(int blockCount)
        {
            //nothing to do
        }
    }
}

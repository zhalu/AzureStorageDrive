using AzureStorageDrive.Util;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public class AzureBlobCopyTarget : ICopyTarget
    {
        public AzureBlobServiceDriveInfo Drive { get; private set; }
        public string BasePath { get; private set; }
        public CloudPageBlob PageBlob { get; private set; }

        public CloudBlockBlob BlockBlob { get; private set; }

        public PathType TargetType { get; set; }

        public AzureBlobCopyTarget(AzureBlobServiceDriveInfo drive, string basePath)
        {
            this.Drive = drive;
            this.BasePath = basePath;
            this.TargetType = PathType.AzureBlobPage;
        }

        public bool Prepare(string name, long size)
        {
            if (size > Constants.TB)
            {
                return false;
            }

            if (size % 512 > 0)
            {
                this.TargetType = PathType.AzureBlobBlock;
            }

            var r = AzureBlobPathResolver.ResolvePath(this.Drive.Client, this.BasePath, skipCheckExistence: false);
            if (r.PathType == PathType.AzureBlobDirectory)
            {
                if (this.TargetType == PathType.AzureBlobPage) {
                    this.PageBlob = r.Directory.GetPageBlobReference(name);
                    this.PageBlob.Create(size);
                }
                else
                {
                    this.BlockBlob = r.Directory.GetBlockBlobReference(name);
                    //No need for creating block blob
                }

                return true;
            }

            //TODO: need to support user specify the target file name
            return false;
        }

        public void Go(byte[] bytes, int offset, int count, long targetOffset, int blockId)
        {

            using (var m = new MemoryStream(bytes, (int)offset, count))
            {
                if (TargetType == PathType.AzureBlobPage)
                {
                    this.PageBlob.WritePages(m, targetOffset);
                }
                else
                {
                    try
                    {
                        this.BlockBlob.PutBlock(Utils.GetBlockId(blockId), m, null);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            } 
        }

        public void Done(int blockCount)
        {
            //commit block list, no need for page blob
            if (TargetType == PathType.AzureBlobBlock)
            {
                this.BlockBlob.PutBlockList(Utils.GetBlockIdArray(blockCount));
            }
        }
    }
}

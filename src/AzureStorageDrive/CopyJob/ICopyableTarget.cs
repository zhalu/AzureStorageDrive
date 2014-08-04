using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public interface ICopyableTarget
    {
        void UploadRange(object fileObject, byte[] buffer, int offset, int count, long targetOffset, long blockLabel);

        void UploadCompleted(object fileObject, int totalBlocksCount);

        bool BeforeUploadingFile(string basePath, string relativePath, long size, bool isTransferringSingFile, out object fileObject);

        void CreateDirectory(string basePath, string relativePath);
    }
}

using AzureStorageDrive.CopyJob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive
{
    public abstract class AbstractDriveInfo : ICopyableSource, ICopyableTarget
    {
        public CmdletProvider RootProvider { get; set; }
        public string Name { get; set; }
        public abstract void NewItem(
                                    string path,
                                    string type,
                                    object newItemValue);
        public abstract void GetChildItems(string path, bool recurse);
        public abstract bool HasChildItems(string path);
        public abstract bool IsValidPath(string path);
        public abstract bool ItemExists(string path);
        public abstract void RemoveItem(string path, bool recurse);
        public abstract bool IsItemContainer(string path);
        public abstract IContentReader GetContentReader(string path);
        public abstract void GetChildNames(string path, ReturnContainers returnContainers);

        public abstract void GetProperty(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList);

        public abstract void SetProperty(string path, PSObject propertyValue);

        protected static Dictionary<string, string> ParseValues(string str)
        {
            var dict = new Dictionary<string, string>();
            var sep = new char[] { '=' };
            var parts = str.Split('&');
            foreach (var p in parts)
            {
                var pair = p.Split(sep, 2);
                dict.Add(pair[0].ToLowerInvariant(), pair[1]);
            }

            return dict;
        }

        public virtual IEnumerable<CopySourceItem> ListFilesForCopy(string path, bool recurse, List<string> baseParts = null)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsRenameSupported()
        {
            throw new NotImplementedException();
        }

        public virtual void DownloadRange(CopySourceItem copySourceItem, byte[] target, int index, long fileOffset, int length)
        {
            throw new NotImplementedException();
        }

        public virtual void UploadRange(object fileObject, byte[] buffer, int offset, int count, long targetOffset, long blockLabel)
        {
            throw new NotImplementedException();
        }

        public virtual void UploadCompleted(object fileObject, int totalBlocksCount)
        {
            throw new NotImplementedException();
        }

        public virtual bool BeforeUploadingFile(string basePath, string relativePath, long size, bool isTransferringSingFile, out object fileObject)
        {
            throw new NotImplementedException();
        }

        public virtual void CreateDirectory(string basePath, string relativePath)
        {
            throw new NotImplementedException();
        }
    }
}

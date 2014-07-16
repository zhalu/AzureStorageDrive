using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive
{
    public abstract class AbstractDriveInfo
    {
        public string Name { get; set; }
        public abstract void NewItem(
                                    string path,
                                    string type,
                                    object newItemValue);
        public abstract void GetChildItems(string path, bool recurse);
        public abstract bool HasChildItems(string path);
        public abstract bool IsValidPath(string path);
        public abstract bool ItemExists(string path);
        public abstract bool IsItemContainer(string path);
        public abstract IContentReader GetContentReader(string path);

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
    }
}

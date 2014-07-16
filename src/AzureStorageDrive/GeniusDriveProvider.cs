using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive
{
    [CmdletProvider("GeniusDrive", ProviderCapabilities.None)]
    public class GeniusDriveProvider : NavigationCmdletProvider, IContentCmdletProvider, IPropertyCmdletProvider
    {
        private Dictionary<string, AbstractDriveInfo> Drives = new Dictionary<string, AbstractDriveInfo>();

        #region DriveCmdlet
        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            // Check if the drive object is null.
            if (drive == null)
            {
                WriteError(new ErrorRecord(
                           new ArgumentNullException("drive"),
                           "NullDrive",
                           ErrorCategory.InvalidArgument,
                           null));

                return null;
            }

            var d = new PSDriveInfo(name: drive.Name, provider: null, root: PathResolver.Root, description: null, credential: null);

            return d;
        }

        protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
        {
            if (drive == null)
            {
                WriteError(new ErrorRecord(
                   new ArgumentNullException("drive"),
                   "NullDrive",
                   ErrorCategory.InvalidArgument,
                   drive));

                return null;
            }

            return drive;
        }

        #endregion

        #region ContainerCmdletProvider
        protected override void GetChildItems(string path, bool recurse)
        {
            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0) //if listing root, then list all drives
            {
                foreach (var pair in Drives)
                {
                    WriteItemObject(pair.Value, path, true);
                    if (recurse)
                    {
                        pair.Value.GetChildItems(PathResolver.GetSubpath(path), recurse);
                    }
                }

                return;
            }
            else
            {
                var drive = GetDrive(parts[0]);
                if (drive == null)
                {
                    return;
                }

                drive.GetChildItems(PathResolver.GetSubpath(path), recurse);
            }
        }

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0)
            {
                return;
            }

            foreach (var pair in Drives)
            {
                WriteItemObject(pair.Key, "/", true);
            }
        }

        protected override bool HasChildItems(string path)
        {
            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0)
            {
                return Drives.Count > 0;
            }

            var drive = GetDrive(parts[0]);
            if (drive == null)
            {
                return false;
            }

            return drive.HasChildItems(PathResolver.GetSubpath(path));
        }

        protected override void NewItem(
                                    string path,
                                    string type,
                                    object newItemValue)
        {
            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0)
            {
                return;
            }

            //if the provider is not mounted, then create it first
            var drive = GetDrive(parts[0]);
            if (drive == null)
            {
                drive = DriveFactory.CreateInstance(type, newItemValue, this);
                if (drive == null)
                {
                    WriteError(new ErrorRecord(
                        new Exception("Unregcognized type"),
                        "NewItem type",
                        ErrorCategory.InvalidArgument,
                        ""));
                    return;
                }
                else
                {
                    Drives.Add(type, drive);
                }
            }

            //delegate it
            drive.NewItem(PathResolver.GetSubpath(path), type, newItemValue);
            return;
        }

        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            throw new NotImplementedException();
        }

        protected override void RemoveItem(string path, bool recurse)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region ItemCmdlet

        protected override void GetItem(string path)
        {
            GetChildItems(path, false);
        }

        protected override void SetItem(string path, object value)
        {
            throw new NotImplementedException();
        }

        protected override bool IsValidPath(string path)
        {
            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0)
            {
                return true;
            }

            //if the provider is not mounted, then create it first
            var drive = GetDrive(parts[0]);
            if (drive == null)
            {
                return false;
            }

            //delegate it
            return drive.IsValidPath(PathResolver.GetSubpath(path));
        }

        protected override bool ItemExists(string path)
        {
            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0)
            {
                return true;
            }

            //if the provider is not mounted, then create it first
            var drive = GetDrive(parts[0]);
            if (drive == null)
            {
                return false;
            }

            //delegate it
            return drive.ItemExists(PathResolver.GetSubpath(path));
        }
        #endregion

        #region private helper

        private AbstractDriveInfo GetDrive(string mountedLabel)
        {
            var key = Drives.Keys.Where(p => string.Equals(mountedLabel, p, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (key != default(string))
            {
                return Drives[key];
            }

            return null;
        }
        #endregion

        #region navigation
        protected override string GetChildName(string path)
        {
            if (path == this.PSDriveInfo.Root)
            {
                return this.PSDriveInfo.Root;
            }

            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0)
            {
                return this.PSDriveInfo.Root;
            }

            return parts.Last();
        }
        protected override string GetParentPath(string path, string root)
        {
            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0)
            {
                return string.Empty;
            }

            if (parts.Count <= 1)
            {
                if (path.StartsWith(PathResolver.Root))
                {
                    return string.Empty;
                }
                else
                {
                    return PathResolver.Root;
                }
            }

            parts.RemoveAt(parts.Count - 1);
            var r = string.Join(PathResolver.PathSeparator, parts.ToArray());
            return r;
        }
        protected override bool IsItemContainer(string path)
        {
            if (PathResolver.IsLocalPath(path))
            {
                return true;
            }

            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0)
            {
                return true;
            }

            //if the provider is not mounted, then create it first
            var drive = GetDrive(parts[0]);
            if (drive == null)
            {
                return false;
            }

            if (parts.Count == 1)
            {
                return true;
            }

            //delegate it
            return drive.IsItemContainer(PathResolver.GetSubpath(path));
        }
        protected override string MakePath(string parent, string child)
        {
            var result = PathResolver.Combine(parent, child);
            var parts = PathResolver.SplitPath(result);
            result = PathResolver.Combine(parts);

            if (parent.StartsWith(PathResolver.Root))
            {
                result = PathResolver.Root + result;
            }

            return result;
        }
        protected override void MoveItem(string path, string destination)
        {
            CopyItem(path, destination, true);
            WriteWarning("Files/Directories have been copied.");
        }
        protected override object MoveItemDynamicParameters(string path, string destination)
        {
            return null;
        }
        protected override string NormalizeRelativePath(string path, string basePath)
        {
            if (!string.IsNullOrEmpty(basePath) && path.StartsWith(basePath, StringComparison.InvariantCultureIgnoreCase))
            {
                var pathTrimmed = path.Substring(basePath.Length);
                return PathResolver.NormalizePath(pathTrimmed);
            }

            return PathResolver.NormalizePath(path);
        }
        #endregion

        #region content cmdlets
        public void ClearContent(string path)
        {
            throw new NotImplementedException();
        }

        public object ClearContentDynamicParameters(string path)
        {
            throw new NotImplementedException();
        }

        public IContentReader GetContentReader(string path)
        {
            if (PathResolver.IsLocalPath(path))
            {
                return null;
            }

            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0)
            {
                return null;
            }

            //if the provider is not mounted, then create it first
            var drive = GetDrive(parts[0]);
            if (drive == null)
            {
                return null;
            }
            
            //delegate it
            return drive.GetContentReader(PathResolver.GetSubpath(path));
        }

        public object GetContentReaderDynamicParameters(string path)
        {
            return null;
        }

        public IContentWriter GetContentWriter(string path)
        {
            throw new NotImplementedException();
        }

        public object GetContentWriterDynamicParameters(string path)
        {
            throw new NotImplementedException();
        }

        #endregion

        public void ClearProperty(string path, System.Collections.ObjectModel.Collection<string> propertyToClear)
        {
            throw new NotImplementedException();
        }

        public object ClearPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> propertyToClear)
        {
            throw new NotImplementedException();
        }

        #region IPropertyCmdletProvider
        public void GetProperty(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList)
        {
            if (PathResolver.IsLocalPath(path))
            {
                return;
            }

            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0)
            {
                return;
            }

            //if the provider is not mounted, then create it first
            var drive = GetDrive(parts[0]);
            if (drive == null)
            {
                return;
            }

            //delegate it
            drive.GetProperty(PathResolver.GetSubpath(path), providerSpecificPickList);
        }

        public object GetPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList)
        {
            return null;
        }

        public void SetProperty(string path, PSObject propertyValue)
        {
            if (PathResolver.IsLocalPath(path))
            {
                return;
            }

            var parts = PathResolver.SplitPath(path);
            if (parts.Count == 0)
            {
                return;
            }

            //if the provider is not mounted, then create it first
            var drive = GetDrive(parts[0]);
            if (drive == null)
            {
                return;
            }

            //delegate it
            drive.SetProperty(PathResolver.GetSubpath(path), propertyValue);
            //var r = PathResolver.ResolvePath(this.FileDrive.Client, path, skipCheckExistence: false);
            //switch (r.PathType)
            //{
            //    case PathType.AzureFile:
            //        r.File.FetchAttributes();
            //        MergeProperties(r.File.Metadata, propertyValue.Properties);
            //        r.File.SetMetadata();
            //        break;
            //    case PathType.AzureFileDirectory:
            //        if (r.Parts.Count() == 1)
            //        {
            //            r.Share.FetchAttributes();
            //            MergeProperties(r.Share.Metadata, propertyValue.Properties);
            //            r.Share.SetMetadata();
            //        }
            //        else
            //        {
            //            throw new Exception("Setting metadata/properties for directory is not supported");
            //        }
            //        break;
            //    default:
            //        break;
            //}
        }

        //private void MergeProperties(IDictionary<string, string> target, PSMemberInfoCollection<PSPropertyInfo> source)
        //{
        //    foreach (var info in source)
        //    {
        //        var name = info.Name;
        //        if (target.ContainsKey(name))
        //        {
        //            target.Remove(name);
        //        }

        //        target.Add(name, info.Value.ToString());
        //    }
        //}

        public object SetPropertyDynamicParameters(string path, PSObject propertyValue)
        {
            return null;
        }
        #endregion

    }
}

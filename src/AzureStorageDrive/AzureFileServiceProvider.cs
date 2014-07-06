using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AzureStorageDrive
{
    [CmdletProvider("AzureFileDrive", ProviderCapabilities.None)]
    public class AzureFileServiceProvider : NavigationCmdletProvider, IContentCmdletProvider, IPropertyCmdletProvider
    {
        private AzureFileServiceDriveInfo FileDrive
        {
            get
            {
                return this.PSDriveInfo as AzureFileServiceDriveInfo;
            }
        }

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

            // Check if the drive object is null.
            if (string.IsNullOrWhiteSpace(drive.Root))
            {
                WriteError(new ErrorRecord(
                           new ArgumentNullException("root"),
                           "NoRoot",
                           ErrorCategory.InvalidArgument,
                           null));

                return null;
            }

            var newDrive = AzureFileServiceDriveInfo.Parse(drive);
            return newDrive;
        }

        protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
        {
            if (drive == null) {
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
            var folders = recurse ? new List<string>() : null;

            var items = this.FileDrive.ListItems(path);
            this.FileDrive.HandleItems(items,
                (f) =>
                {
                    WriteItemObject(f, path, true);
                },
                (d) =>
                {
                    WriteItemObject(d, path, true);
                    if (recurse)
                    {
                        var p = PathResolver.Combine(path, d.Name);
                        folders.Add(p);
                    }
                },
                (s) =>
                {
                    WriteItemObject(s, path, true);
                    if (recurse)
                    {
                        var p = PathResolver.Combine(path, s.Name);
                        folders.Add(p);
                    }
                });

            if (recurse && folders != null)
            {
                foreach (var f in folders)
                {
                    GetChildItems(f, recurse);
                }
            }
        }

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            var r = PathResolver.ResolvePath(this.FileDrive.Client, path);
            switch (r.PathType)
            {
                case PathType.Root:
                    var shares = this.FileDrive.ListItems(path);
                    foreach (CloudFileShare s in shares)
                    {
                        WriteItemObject(s.Name, path, true);
                    }
                    break;
                case PathType.Directory:
                    var items = r.Directory.ListFilesAndDirectories();
                    var parentPath = PathResolver.Combine(r.Parts);
                    this.FileDrive.HandleItems(items,
                        (f) => WriteItemObject(f.Name, PathResolver.Root, false),
                        (d) => WriteItemObject(d.Name, PathResolver.Root, true),
                        (s) => { }
                        );
                    break;
                case PathType.File:
                default:
                    break;
            } 
        }

        protected override bool HasChildItems(string path)
        {
            var r = PathResolver.ResolvePath(this.FileDrive.Client, path, hint: PathType.Directory, skipCheckExistence: false);
            return r.Exists();
        }

        protected override void NewItem(
                                    string path, 
                                    string type,
                                    object newItemValue)
        {
            if (string.Equals(type, "Directory", StringComparison.InvariantCultureIgnoreCase))
            {
                this.FileDrive.CreateDirectory(path);
            }
            else if (string.Equals(type, "EmptyFile", StringComparison.InvariantCultureIgnoreCase))
            {
                if (newItemValue != null)
                {
                    var size = 0L;
                    if (long.TryParse(newItemValue.ToString(), out size))
                    {
                        this.FileDrive.CreateEmptyFile(path, size);
                    }
                    else
                    {
                        this.FileDrive.CreateEmptyFile(path, 0);
                    }
                }
            }
            else
            {
                var parts = PathResolver.SplitPath(path);
                if (parts.Count == 1)
                {
                    this.FileDrive.CreateShare(parts[0]);
                }
                else
                {
                    this.FileDrive.CreateFile(path, newItemValue.ToString());
                }
            }
        }

        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            if (PathResolver.IsLocalPath(copyPath))
            {
                this.FileDrive.Download(path, PathResolver.ConvertToRealLocalPath(copyPath));

            }
            else if (PathResolver.IsLocalPath(path))
            {
                this.FileDrive.Upload(PathResolver.ConvertToRealLocalPath(path), copyPath);
            }
        }

        protected override void RemoveItem(string path, bool recurse)
        {
            var r = PathResolver.ResolvePath(this.FileDrive.Client, path, skipCheckExistence: false);
            switch (r.PathType)
            {
                case PathType.Directory:
                    
                    this.FileDrive.DeleteDirectory(r.Directory, recurse);
                    break;
                case PathType.File:
                    r.File.Delete();
                    break;
                default:
                    break;
            }
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
            throw new NotImplementedException();
        }

        protected override bool ItemExists(string path)
        {
            if (PathResolver.IsLocalPath(path))
            {
                path = PathResolver.ConvertToRealLocalPath(path);
                return File.Exists(path) || Directory.Exists(path);
            }

            try
            {
                var r = PathResolver.ResolvePath(this.FileDrive.Client, path, skipCheckExistence: false);
                var exists = r.Exists();
                return exists;
            }
            catch (Exception e)
            {
                return false;
            }
        }
 #endregion

        #region private helper
        
        #endregion

        #region navigation
        protected override string GetChildName(string path)
        {
            if (path == PathResolver.Root)
            {
                return PathResolver.Root;
            }

            var parts = PathResolver.SplitPath(path);
            if (parts.Count > 0)
            {
                return parts.Last();
            }

            return PathResolver.Root;
        }
        protected override string GetParentPath(string path, string root)
        {
            var path2 = NormalizeRelativePath(path, root);

            var parts = PathResolver.SplitPath(path2);
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

            try
            {
                var r = PathResolver.ResolvePath(this.FileDrive.Client, path, hint: PathType.Directory, skipCheckExistence: false);
                return r.Exists();
            }
            catch (Exception e)
            {
                return false;
            }
        }
        protected override string MakePath(string parent, string child)
        {
            string result;

            result = PathResolver.Combine(parent, child);
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
            return this.FileDrive.GetReader(path);
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
            var r = PathResolver.ResolvePath(this.FileDrive.Client, path, skipCheckExistence: false);
            switch (r.PathType)
            {
                case PathType.File:
                    r.File.FetchAttributes();
                    WriteItemObject(r.File.Properties, path, false);
                    WriteItemObject(r.File.Metadata, path, false);
                    break;
                case PathType.Directory:
                    if (r.Parts.Count() == 1) {
                        r.Share.FetchAttributes();
                        WriteItemObject(r.Share.Properties, path, true);
                        WriteItemObject(r.Share.Metadata, path, true);
                    }
                    else
                    {
                        r.Directory.FetchAttributes();
                        WriteItemObject(r.Directory.Properties, path, true);
                    }
                    break;
                default:
                    break;
            }
        }

        public object GetPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList)
        {
            return null;
        }

        public void SetProperty(string path, PSObject propertyValue)
        {
            var r = PathResolver.ResolvePath(this.FileDrive.Client, path, skipCheckExistence: false);
            switch (r.PathType)
            {
                case PathType.File:
                    r.File.FetchAttributes();
                    MergeProperties(r.File.Metadata, propertyValue.Properties);
                    r.File.SetMetadata();
                    break;
                case PathType.Directory:
                    if (r.Parts.Count() == 1)
                    {
                        r.Share.FetchAttributes();
                        MergeProperties(r.Share.Metadata, propertyValue.Properties);
                        r.Share.SetMetadata();
                    }
                    else
                    {
                        throw new Exception("Setting metadata/properties for directory is not supported");
                    }
                    break;
                default:
                    break;
            }
        }

        private void MergeProperties(IDictionary<string, string> target, PSMemberInfoCollection<PSPropertyInfo> source)
        {
            foreach (var info in source)
            {
                var name = info.Name;
                if (target.ContainsKey(name))
                {
                    target.Remove(name);
                }

                target.Add(name, info.Value.ToString());
            }
        }

        public object SetPropertyDynamicParameters(string path, PSObject propertyValue)
        {
            return null;
        }
        #endregion
    }
}

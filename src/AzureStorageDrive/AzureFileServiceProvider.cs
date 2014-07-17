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
    //[CmdletProvider("AzureFileDrive", ProviderCapabilities.None)]
    //public class AzureFileServiceProvider : AbstractDriveInfo
    //{
 //       private AzureFileServiceDriveInfo FileDrive
 //       {
 //           get
 //           {
 //               return null;
 //           }
 //       }

 //       #region DriveCmdlet
 //       public override PSDriveInfo NewDrive(PSDriveInfo drive)
 //       {
 //           // Check if the drive object is null.
 //           if (drive == null)
 //           {
 //               WriteError(new ErrorRecord(
 //                          new ArgumentNullException("drive"),
 //                          "NullDrive",
 //                          ErrorCategory.InvalidArgument,
 //                          null));

 //               return null;
 //           }

 //           // Check if the drive object is null.
 //           if (string.IsNullOrWhiteSpace(drive.Root))
 //           {
 //               WriteError(new ErrorRecord(
 //                          new ArgumentNullException("root"),
 //                          "NoRoot",
 //                          ErrorCategory.InvalidArgument,
 //                          null));

 //               return null;
 //           }

 //           throw new NotImplementedException();
 //       }

 //       public override PSDriveInfo RemoveDrive(PSDriveInfo drive)
 //       {
 //           if (drive == null) {
 //               WriteError(new ErrorRecord(
 //                  new ArgumentNullException("drive"), 
 //                  "NullDrive",
 //                  ErrorCategory.InvalidArgument, 
 //                  drive));

 //               return null;
 //           }

 //           return drive;
 //       }

 //       #endregion

        






 //       public override void CopyItem(string path, string copyPath, bool recurse)
 //       {
 //           if (PathResolver.IsLocalPath(copyPath))
 //           {
 //               this.FileDrive.Download(path, PathResolver.ConvertToRealLocalPath(copyPath));

 //           }
 //           else if (PathResolver.IsLocalPath(path))
 //           {
 //               this.FileDrive.Upload(PathResolver.ConvertToRealLocalPath(path), copyPath);
 //           }
 //       }


 //       #endregion

 //       #region ItemCmdlet
       
 //       public override void GetItem(string path)
 //       {
 //           GetChildItems(path, false);
 //       }

 //       public override void SetItem(string path, object value)
 //       {
 //           throw new NotImplementedException();
 //       }

 //       public override bool IsValidPath(string path)
 //       {
 //           throw new NotImplementedException();
 //       }

 //       public override bool ItemExists(string path)
 //       {
 //           if (PathResolver.IsLocalPath(path))
 //           {
 //               path = PathResolver.ConvertToRealLocalPath(path);
 //               return File.Exists(path) || Directory.Exists(path);
 //           }

 //           try
 //           {
 //               var r = AzureFilePathResolver.ResolvePath(this.FileDrive.Client, path, skipCheckExistence: false);
 //               var exists = r.Exists();
 //               return exists;
 //           }
 //           catch (Exception e)
 //           {
 //               return false;
 //           }
 //       }
 //#endregion

 //       #region private helper
        
 //       #endregion

 //       #region navigation
 //       public override string GetChildName(string path)
 //       {
 //           if (path == PathResolver.Root)
 //           {
 //               return PathResolver.Root;
 //           }

 //           var parts = PathResolver.SplitPath(path);
 //           if (parts.Count > 0)
 //           {
 //               return parts.Last();
 //           }

 //           return PathResolver.Root;
 //       }
 //       public override string GetParentPath(string path, string root)
 //       {
 //           var path2 = NormalizeRelativePath(path, root);

 //           var parts = PathResolver.SplitPath(path2);
 //           if (parts.Count <= 1)
 //           {
 //               if (path.StartsWith(PathResolver.Root))
 //               {
 //                   return string.Empty;
 //               }
 //               else
 //               {
 //                   return PathResolver.Root;
 //               }
 //           }

 //           parts.RemoveAt(parts.Count - 1);
 //           var r = string.Join(PathResolver.PathSeparator, parts.ToArray());
 //           return r;
 //       }
 //       public override bool IsItemContainer(string path)
 //       {
 //           if (PathResolver.IsLocalPath(path))
 //           {
 //               return true;
 //           }

 //           var parts = PathResolver.SplitPath(path);
 //           if (parts.Count == 0)
 //           {
 //               return true;
 //           }

 //           try
 //           {
 //               var r = AzureFilePathResolver.ResolvePath(this.FileDrive.Client, path, hint: PathType.AzureFileDirectory, skipCheckExistence: false);
 //               return r.Exists();
 //           }
 //           catch (Exception e)
 //           {
 //               return false;
 //           }
 //       }
 //       public override string MakePath(string parent, string child)
 //       {
 //           string result;

 //           result = PathResolver.Combine(parent, child);
 //           var parts = PathResolver.SplitPath(result);
 //           result = PathResolver.Combine(parts);

 //           if (parent.StartsWith(PathResolver.Root))
 //           {
 //               result = PathResolver.Root + result;
 //           }

 //           return result;
 //       }
 //       public override void MoveItem(string path, string destination)
 //       {
 //           CopyItem(path, destination, true);
 //           WriteWarning("Files/Directories have been copied.");
 //       }
 //       public override object MoveItemDynamicParameters(string path, string destination)
 //       {
 //           return null;
 //       }
 //       public override string NormalizeRelativePath(string path, string basePath)
 //       {
 //           if (!string.IsNullOrEmpty(basePath) && path.StartsWith(basePath, StringComparison.InvariantCultureIgnoreCase))
 //           {
 //               var pathTrimmed = path.Substring(basePath.Length);
 //               return PathResolver.NormalizePath(pathTrimmed);
 //           }

 //           return PathResolver.NormalizePath(path);
 //       }
 //       #endregion

 //       #region content cmdlets
 //       public void ClearContent(string path)
 //       {
 //           throw new NotImplementedException();
 //       }

 //       public object ClearContentDynamicParameters(string path)
 //       {
 //           throw new NotImplementedException();
 //       }

 //       public IContentReader GetContentReader(string path)
 //       {
 //           return this.FileDrive.GetReader(path);
 //       }

 //       public object GetContentReaderDynamicParameters(string path)
 //       {
 //           return null;
 //       }

 //       public IContentWriter GetContentWriter(string path)
 //       {
 //           throw new NotImplementedException();
 //       }

 //       public object GetContentWriterDynamicParameters(string path)
 //       {
 //           throw new NotImplementedException();
 //       }

 //       #endregion

 //       public void ClearProperty(string path, System.Collections.ObjectModel.Collection<string> propertyToClear)
 //       {
 //           throw new NotImplementedException();
 //       }

 //       public object ClearPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> propertyToClear)
 //       {
 //           throw new NotImplementedException();
 //       }

 //       #region IPropertyCmdletProvider
 //       public void GetProperty(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList)
 //       {
 //           var r = AzureFilePathResolver.ResolvePath(this.FileDrive.Client, path, skipCheckExistence: false);
 //           switch (r.PathType)
 //           {
 //               case PathType.AzureFile:
 //                   r.File.FetchAttributes();
 //                   WriteItemObject(r.File.Properties, path, false);
 //                   WriteItemObject(r.File.Metadata, path, false);
 //                   break;
 //               case PathType.AzureFileDirectory:
 //                   if (r.Parts.Count() == 1) {
 //                       r.Share.FetchAttributes();
 //                       WriteItemObject(r.Share.Properties, path, true);
 //                       WriteItemObject(r.Share.Metadata, path, true);
 //                   }
 //                   else
 //                   {
 //                       r.Directory.FetchAttributes();
 //                       WriteItemObject(r.Directory.Properties, path, true);
 //                   }
 //                   break;
 //               default:
 //                   break;
 //           }
 //       }

 //       public object GetPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList)
 //       {
 //           return null;
 //       }

 //       public void SetProperty(string path, PSObject propertyValue)
 //       {
 //           var r = AzureFilePathResolver.ResolvePath(this.FileDrive.Client, path, skipCheckExistence: false);
 //           switch (r.PathType)
 //           {
 //               case PathType.AzureFile:
 //                   r.File.FetchAttributes();
 //                   MergeProperties(r.File.Metadata, propertyValue.Properties);
 //                   r.File.SetMetadata();
 //                   break;
 //               case PathType.AzureFileDirectory:
 //                   if (r.Parts.Count() == 1)
 //                   {
 //                       r.Share.FetchAttributes();
 //                       MergeProperties(r.Share.Metadata, propertyValue.Properties);
 //                       r.Share.SetMetadata();
 //                   }
 //                   else
 //                   {
 //                       throw new Exception("Setting metadata/properties for directory is not supported");
 //                   }
 //                   break;
 //               default:
 //                   break;
 //           }
 //       }

 //       private void MergeProperties(IDictionary<string, string> target, PSMemberInfoCollection<PSPropertyInfo> source)
 //       {
 //           foreach (var info in source)
 //           {
 //               var name = info.Name;
 //               if (target.ContainsKey(name))
 //               {
 //                   target.Remove(name);
 //               }

 //               target.Add(name, info.Value.ToString());
 //           }
 //       }

 //       public object SetPropertyDynamicParameters(string path, PSObject propertyValue)
 //       {
 //           return null;
 //       }
 //       #endregion
 //   }
}

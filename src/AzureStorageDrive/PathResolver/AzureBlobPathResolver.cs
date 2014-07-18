﻿using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive
{
    public class AzureBlobPathResolver : PathResolver
    {
        public static AzureBlobPathResolveResult ResolvePath(CloudBlobClient client, string path, PathType hint = PathType.Unknown, bool skipCheckExistence = true)
        {
            var result = new AzureBlobPathResolveResult();
            var parts = SplitPath(path);
            if (!ValidatePath(parts))
            {
                throw new Exception("Path " + path + " is invalid");
            }

            result.Parts = parts;

            if (parts.Count == 0)
            {
                result.PathType = PathType.AzureBlobRoot;
            }

            if (parts.Count > 0)
            {
                result.Container = client.GetContainerReference(parts[0]);
                result.Directory = result.Container.GetDirectoryReference("");
                result.RootDirectory = result.Directory;
                result.PathType = PathType.AzureBlobDirectory;

                if (!skipCheckExistence && !result.Container.Exists())
                {
                    result.PathType = PathType.Invalid;
                    return result;
                }
            }

            if (parts.Count > 1)
            {
                for (var level = 1; level < parts.Count - 1; ++level)
                {
                    //assume it's directory
                    var dir = result.Directory.GetDirectoryReference(parts[level]);
                    if (result.PathType == PathType.AzureFileDirectory)
                    {
                        result.Directory = dir;
                        result.PathType = PathType.AzureFileDirectory;
                        continue;
                    }
                }

                //last element
                if (parts.Count > 1)
                {
                    if (hint == PathType.AzureBlobDirectory || hint == PathType.Unknown)
                    {
                        //assume it's directory first
                        var dir = result.Directory.GetDirectoryReference(parts.Last());
                        if (result.PathType == PathType.AzureFileDirectory 
                            && (skipCheckExistence || dir.ListBlobsSegmented(false, BlobListingDetails.None, 1, null, null, null).Results.Count() > 0))
                        {
                            result.Directory = dir;
                            result.PathType = PathType.AzureBlobDirectory;
                            return result;
                        }

                    }

                    //2. assume it's a block blob
                    if (hint == PathType.AzureBlobBlock || hint == PathType.Unknown)
                    {
                        var blob = result.Directory.GetBlockBlobReference(parts.Last());
                        if (result.PathType == PathType.AzureBlobDirectory && (skipCheckExistence || blob != null))
                        {
                            result.Blob = blob as ICloudBlob;
                            result.PathType = PathType.AzureBlobBlock;
                            return result;
                        }
                    }

                    //3. assume it's a page blob
                    if (hint == PathType.AzureBlobPage || hint == PathType.Unknown)
                    {
                        var blob = result.Directory.GetPageBlobReference(parts.Last());
                        if (result.PathType == PathType.AzureBlobDirectory && (skipCheckExistence || blob != null))
                        {
                            result.Blob = blob as ICloudBlob;
                            result.PathType = PathType.AzureBlobPage;
                            return result;
                        }
                    }
                }

                result.PathType = PathType.Unknown;
            }

            if (result.PathType == PathType.AzureBlobDirectory && hint == PathType.AzureBlobBlock)
            {
                result.PathType = PathType.Invalid;
            }

            return result;
        }

        public static bool ValidatePath(List<string> parts)
        {
            if (parts.Count == 0)
            {
                return true;
            }

            //todo: add more checks here
            return true;
        }
    }
}

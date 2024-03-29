﻿using Aliyun.OpenServices.OpenStorageService;
using AzureStorageDrive.CopyJob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive
{
    public class AliOssServiceDriveInfo : AbstractDriveInfo
    {
        public OssClient Client { get; set; }

        public string AccountName { get; set; }

        public AliOssServiceDriveInfo(string credential, string name)
        {
            this.Name = name;
            var dict = ParseValues(credential);
            var accountName = dict["account"];
            var accountKey = dict["key"];

            this.Client = new OssClient(accountName, accountKey);
            this.AccountName = accountName;
        }

        public override void NewItem(string path, string type, object newItemValue)
        {
            throw new NotImplementedException();
        }

        public override void GetChildItems(string path, bool recurse)
        {
            var subPaths = recurse ? new List<string>() : null;

            var items = this.ListItems(path);
            foreach (var item in items)
            {
                if (item is Bucket)
                {
                    this.RootProvider.WriteItemObject(item, path, true);
                    if (recurse)
                    {
                        Bucket b = item as Bucket;
                        subPaths.Add(b.Name);
                    }
                }
                else if (item is OssObjectSummary)
                {
                    this.RootProvider.WriteItemObject(item, path, false);
                    if (recurse)
                    {
                        OssObjectSummary o = item as OssObjectSummary;
                        if (o.Key.EndsWith("/"))
                        {
                            subPaths.Add(o.BucketName + @"\" + o.Key.Replace("/", PathResolver.PathSeparator));
                        }
                    }
                }
            }

            if (recurse)
            {
                foreach (string subPath in subPaths)
                {
                    GetChildItems(subPath, true);
                }
            }
        }

        internal IEnumerable<object> ListItems(string path)
        {
            var result = AliOssPathResolver.ResolvePath(path);

            switch (result.PathType)
            {
                case AliOssPathType.Root:
                    return ListBuckets();
                case AliOssPathType.Bucket:
                case AliOssPathType.Object:
                    List<object> objs = new List<object>();
                    objs.AddRange(ListObjects(result.Bucket, result.Prefix));
                    return objs;
                default:
                    return null;
            }
        }

        private IEnumerable<Bucket> ListBuckets()
        {
            return Client.ListBuckets();
        }

        private IEnumerable<OssObjectSummary> ListObjects(string bucket, string prefix)
        {
            ListObjectsRequest lsObjRequest = new ListObjectsRequest(bucket);
            lsObjRequest.Delimiter = "/";
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/"))
                prefix += "/";
            lsObjRequest.Prefix = prefix;
            ObjectListing objListing = Client.ListObjects(lsObjRequest);

            List<OssObjectSummary> objects = new List<OssObjectSummary>();
            foreach (string subPrefix in objListing.CommonPrefixes)
            {
                ListObjectsRequest subObjRequest = new ListObjectsRequest(bucket);
                subObjRequest.Delimiter = "/";
                subObjRequest.Prefix = prefix + subPrefix;
                ObjectListing subObjListing = Client.ListObjects(subObjRequest);
                objects.Add(subObjListing.ObjectSummaries.First());
            }

            foreach (var s in objListing.ObjectSummaries)
            {
                if(!s.Key.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                    objects.Add(s);
            }

            return objects;
        }

        private IEnumerable<string> ListGroups(string bucket, string prefix)
        {
            ListObjectsRequest lsObjRequest = new ListObjectsRequest(bucket);
            lsObjRequest.Delimiter = "/";
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/"))
                prefix += "/";
            lsObjRequest.Prefix = prefix;
            ObjectListing objListing = Client.ListObjects(lsObjRequest);
            return objListing.CommonPrefixes;
        }

        internal ObjectMetadata GetObjectMetaData(string bucket, string key)
        {
            try
            {
                return Client.GetObjectMetadata(bucket, key);
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse && (ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound)
                    return null;
                throw;
            }
        }

        public override bool HasChildItems(string path)
        {
            /* TODO
             * Check whether has child items in a effecient way
             */
            return true;
        }

        public override bool IsValidPath(string path)
        {
            throw new NotImplementedException();
        }

        public override bool ItemExists(string path)
        {
            var result = AliOssPathResolver.ResolvePath(path);
            switch (result.PathType)
            {
                case AliOssPathType.Root:
                    return true;
                case AliOssPathType.Bucket:
                    var buckets = ListBuckets();
                    return buckets.First(b => b.Name.Equals(result.Bucket, StringComparison.OrdinalIgnoreCase)) != null ? true : false;
                case AliOssPathType.Object:
                    var meta = GetObjectMetaData(result.Bucket, result.Prefix);
                    if (meta != null)
                        return true;
                    if (!result.Prefix.EndsWith("/"))
                        return GetObjectMetaData(result.Bucket, result.Prefix + "/") != null ? true : false;
                    return false;
                default:
                    return false;
            }
        }

        public override void RemoveItem(string path, bool recurse)
        {
            throw new NotImplementedException();
        }

        public override bool IsItemContainer(string path)
        {
            var result = AliOssPathResolver.ResolvePath(path);
            switch (result.PathType)
            {
                case AliOssPathType.Root:
                case AliOssPathType.Bucket:
                    return true;
                case AliOssPathType.Object:
                    return GetObjectMetaData(result.Bucket, result.Prefix + "/") != null ? true : false;
                default:
                    return false;
            }
        }

        public override IContentReader GetContentReader(string path)
        {
            throw new NotImplementedException();
        }

        public override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            var result = AliOssPathResolver.ResolvePath(path);
            switch (result.PathType)
            {
                case AliOssPathType.Root:
                    var buckets = ListBuckets();
                    foreach (Bucket b in buckets)
                    {
                        this.RootProvider.WriteItemObject(b.Name, PathResolver.Root, true);
                    }
                    break;
                case AliOssPathType.Bucket:     
                case AliOssPathType.Object:
                    var objects = ListObjects(result.Bucket, result.Prefix);
                    foreach (OssObjectSummary o in objects)
                    {
                        string[] parts = o.Key.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        this.RootProvider.WriteItemObject(parts.Last(), path, o.Key.EndsWith("/"));
                    }   
                    break;
                default:
                    break;
            }
        }

        public override void GetProperty(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList)
        {
            throw new NotImplementedException();
        }

        public override void SetProperty(string path, PSObject propertyValue)
        {
            throw new NotImplementedException();
        }

    }
}

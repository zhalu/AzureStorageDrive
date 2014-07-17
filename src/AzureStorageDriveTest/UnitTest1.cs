using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzureStorageDrive;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage;

namespace AzureStorageDriveTest
{
    [TestClass]
    public class PathResolverTests
    {
        //private CloudFileClient client;
        //public PathResolverTests()
        //{
        //    var cred = new StorageCredentials("<your test account>", "<your storage key>");
        //    var account = new CloudStorageAccount(cred, null, null, null, fileStorageUri: new StorageUri(new Uri("http://<your test account>.file.core.windows.net")));
        //    client = account.CreateCloudFileClient();
        //}

        //[TestMethod]
        //public void Test1()
        //{
        //    var r = AzureFilePathResolver.ResolvePath(client, "");
        //    Assert.AreEqual(r.PathType, PathType.AzureFileRoot);

        //    r = AzureFilePathResolver.ResolvePath(client, "/");
        //    Assert.AreEqual(r.PathType, PathType.AzureFileRoot);

        //    r = AzureFilePathResolver.ResolvePath(client, "share");
        //    Assert.AreEqual(r.PathType, PathType.AzureFileDirectory);

        //    r = AzureFilePathResolver.ResolvePath(client, "share/hello/world");
        //    Assert.AreEqual(r.PathType, PathType.AzureFileDirectory);

        //    r = AzureFilePathResolver.ResolvePath(client, "share/hello/world", hint: PathType.AzureFile);
        //    Assert.AreEqual(r.PathType, PathType.AzureFile);
        //}
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Provider;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive
{
    public static class DriveFactory
    {

        public static AbstractDriveInfo CreateInstance(string type, object value, string name)
        {
            switch (type.ToLowerInvariant())
            {
                case "azurefile":
                    var d = new AzureFileServiceDriveInfo(value as string, name);
                    return d;
                default:
                    return null;
                    
            }
        }
    }
}
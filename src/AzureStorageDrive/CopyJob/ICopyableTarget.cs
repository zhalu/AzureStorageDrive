﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageDrive.CopyJob
{
    public interface ICopyableTarget
    {
        ICopyTarget GetCopyTarget(string path);
    }
}
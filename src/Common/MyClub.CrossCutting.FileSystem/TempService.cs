// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using MyNet.Utilities.IO;

namespace MyClub.CrossCutting.FileSystem
{
    public class TempService(string root) : DirectoryService(Path.Combine(root, Guid.NewGuid().ToString())), ITempService
    {
    }
}

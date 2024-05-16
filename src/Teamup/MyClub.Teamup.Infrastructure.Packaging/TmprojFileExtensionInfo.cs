// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using MyNet.Utilities.IO.FileExtensions;
using MyClub.CrossCutting.Localization;

namespace MyClub.Teamup.Infrastructure.Packaging
{
    public static class TmprojFileExtensionInfo
    {
        public const string TmprojExtension = ".tmproj";

        public static FileExtensionInfo Tmproj { get; } = new FileExtensionInfo(nameof(MyClubResources.TmprojFile), new[] { TmprojExtension });

        public static bool IsTmproj(this string filename) => Path.GetExtension(filename).Equals($"{TmprojExtension}", StringComparison.InvariantCultureIgnoreCase);
    }
}

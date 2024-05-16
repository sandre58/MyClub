// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using MyClub.CrossCutting.Localization;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Scorer.Infrastructure.Packaging
{
    public static class ScprojFileExtensionInfo
    {
        public const string ScprojExtension = ".scproj";

        public static FileExtensionInfo Scproj { get; } = new FileExtensionInfo(nameof(MyClubResources.ScprojFile), new[] { ScprojExtension });

        public static bool IsScproj(this string filename) => Path.GetExtension(filename).Equals($"{ScprojExtension}", StringComparison.InvariantCultureIgnoreCase);
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Plugins.Base.File;
using MyClub.Plugins.Base.File.ViewModels;
using MyClub.Plugins.Scorer.Import.File.Providers;
using MyClub.Plugins.Scorer.Import.File.Resources;
using MyClub.Scorer.Application.Contracts;
using MyClub.Scorer.Infrastructure.Packaging;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Plugins.Contracts.Dtos;
using MyNet.UI.Resources;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Plugins.Scorer.Import.File
{
    public class ImportStadiumsSourcePlugin : ImportItemsSourcePlugin<StadiumImportDto>, IImportStadiumsSourcePlugin
    {
        private static readonly SampleFile SampleFile = new("import_stadiums_sample.xlsx", FileResources.ImportStadiumsSampleFileTitle, FileResources.ImportSampleFileTmprojDescription, FileExtensionInfoProvider.Excel, ProcessHelper.OpenInExcel);

        public ImportStadiumsSourcePlugin(IReadService readService)
            : base(new StadiumsFileProvider(readService),
                   ScprojFileExtensionInfo.Scproj.Concat(FileExtensionInfoProvider.Excel).Concat(FileExtensionInfoProvider.Csv, UiResources.AllFiles),
                   SampleFile)
        { }
    }
}

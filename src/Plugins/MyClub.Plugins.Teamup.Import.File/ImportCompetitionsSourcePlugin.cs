// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Plugins.Base.File;
using MyClub.Plugins.Base.File.ViewModels;
using MyClub.Plugins.Teamup.Import.File.Providers;
using MyClub.Plugins.Teamup.Import.File.Resources;
using MyClub.Teamup.Application.Contracts;
using MyClub.Teamup.Infrastructure.Packaging;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyNet.UI.Resources;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Plugins.Teamup.Import.File
{
    public class ImportCompetitionsSourcePlugin : ImportItemsSourcePlugin<CompetitionImportDto>, IImportCompetitionsSourcePlugin
    {
        private static readonly SampleFile SampleFile = new("import_competitions_sample.xlsx", FileResources.ImportCompetitionsSampleFileTitle, FileResources.ImportSampleFileTmprojDescription, FileExtensionInfoProvider.Excel, ProcessHelper.OpenInExcel);

        public ImportCompetitionsSourcePlugin(IReadService readService)
            : base(new CompetitionsFileProvider(readService),
                   TmprojFileExtensionInfo.Tmproj.Concat(FileExtensionInfoProvider.Excel).Concat(FileExtensionInfoProvider.Csv, UiResources.AllFiles),
                   SampleFile)
        { }
    }
}

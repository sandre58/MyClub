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
    public class ImportTeamsSourcePlugin : ImportItemsSourcePlugin<TeamImportDto>, IImportTeamsSourcePlugin
    {
        private static readonly SampleFile SampleFile = new("import_teams_sample.xlsx", FileResources.ImportTeamsSampleFileTitle, FileResources.ImportSampleFileTmprojDescription, FileExtensionInfoProvider.Excel, ProcessHelper.OpenInExcel);

        public ImportTeamsSourcePlugin(IReadService readService)
            : base(new TeamsFileProvider(readService),
                   ScprojFileExtensionInfo.Scproj.Concat(FileExtensionInfoProvider.Excel).Concat(FileExtensionInfoProvider.Csv, UiResources.AllFiles),
                   SampleFile)
        { }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Infrastructure.Packaging;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class TeamsImportViewModel : ImportViewModel<TeamImportableViewModel>
    {
        private static readonly SampleFile SampleFile = new("import_teams_sample.xlsx", MyClubResources.ImportTeamsSampleFileTitle, MyClubResources.ImportSampleFileScprojDescription, FileExtensionInfoProvider.Excel, ProcessHelper.OpenInExcel);

        public TeamsImportViewModel(ProjectInfoProvider projectInfoProvider,
            DatabaseService databaseService,
            TeamsImportService teamsImportService,
            TeamService teamService)
            : base(new ImportSelectSourceViewModel<TeamImportableViewModel>(new Dictionary<ImportSource, ImportSourceViewModel>
                                                {
                                                    { ImportSource.Database, new ImportSourceDatabaseViewModel<TeamImportableViewModel>(new TeamsDatabaseProvider(databaseService, teamService)) },
                                                    { ImportSource.File, new ImportSourceFileViewModel<TeamImportableViewModel>(new TeamsFileProvider(teamsImportService, teamService),
                                                                                                                                  ScprojFileExtensionInfo.Scproj.Concat(FileExtensionInfoProvider.Excel).Concat(FileExtensionInfoProvider.Csv, UiResources.AllFiles),
                                                                                                                                  SampleFile
                                                                                                                                  ) }
                                                }),
                  new TeamsImportListParametersProvider())
            => projectInfoProvider.WhenProjectLoaded(x => Reset());
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.IO.FileExtensions;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Infrastructure.Packaging;
using MyClub.CrossCutting.Localization;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Selection;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class TeamsImportViewModel : ImportViewModel<TeamImportableViewModel>
    {
        private static readonly SampleFile SampleFile = new("import_teams_sample.xlsx", MyClubResources.ImportTeamsSampleFileTitle, MyClubResources.ImportSampleFileTmprojDescription, FileExtensionInfoProvider.Excel, ProcessHelper.OpenInExcel);

        public TeamsImportViewModel(ProjectInfoProvider projectInfoProvider,
            DatabaseService databaseService,
            TeamsImportService teamsImportService,
            TeamService teamService)
            : base(projectInfoProvider,
                  new ImportSelectSourceViewModel<TeamImportableViewModel>(new Dictionary<ImportSource, ImportSourceViewModel>
                                                {
                                                    { ImportSource.Database, new ImportSourceDatabaseViewModel<TeamImportableViewModel>(new TeamsDatabaseProvider(databaseService, teamService)) },
                                                    { ImportSource.File, new ImportSourceFileViewModel<TeamImportableViewModel>(new TeamsFileProvider(teamsImportService, teamService),
                                                                                                                                  TmprojFileExtensionInfo.Tmproj.Concat(FileExtensionInfoProvider.Excel).Concat(FileExtensionInfoProvider.Csv, UiResources.AllFiles),
                                                                                                                                  SampleFile
                                                                                                                                  ) }
                                                }),
                  new TeamsSelectionListParametersProvider())
        { }
    }
}

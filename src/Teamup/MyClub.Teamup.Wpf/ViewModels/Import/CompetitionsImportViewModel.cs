// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reactive.Linq;
using MyNet.UI.Commands;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.IO.FileExtensions;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Infrastructure.Packaging;
using MyClub.CrossCutting.Localization;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class CompetitionsImportViewModel : ImportViewModel<CompetitionImportableViewModel>
    {
        private static readonly SampleFile SampleFile = new("import_competitions_sample.xlsx", MyClubResources.ImportCompetitionsSampleFileTitle, MyClubResources.ImportSampleFileTmprojDescription, FileExtensionInfoProvider.Excel, ProcessHelper.OpenInExcel);

        public CompetitionsImportViewModel(
            ProjectInfoProvider projectInfoProvider,
            DatabaseService databaseService,
            CompetitionsImportService competitionsImportService,
            CompetitionService competitionService) : base(
                projectInfoProvider,
                new ImportSelectSourceViewModel<CompetitionImportableViewModel>(new Dictionary<ImportSource, ImportSourceViewModel>
                                                {
                                                    { ImportSource.Database, new ImportSourceDatabaseViewModel<CompetitionImportableViewModel>(new CompetitionsDatabaseProvider(databaseService, competitionService)) },
                                                    { ImportSource.File, new ImportSourceFileViewModel<CompetitionImportableViewModel>(new CompetitionsFileProvider(competitionsImportService, competitionService),
                                                                                                                                  TmprojFileExtensionInfo.Tmproj.Concat(FileExtensionInfoProvider.Excel).Concat(FileExtensionInfoProvider.Csv, UiResources.AllFiles),
                                                                                                                                  SampleFile
                                                                                                                                  ) }
                                                }),
                new CompetitionsImportListParametersProvider())
        {
            ClearStartDateCommand = CommandsManager.Create(() => SetValueInSelectedRows(y => y.StartDate = null));
            ClearEndDateCommand = CommandsManager.Create(() => SetValueInSelectedRows(y => y.EndDate = null));
            ClearLogoCommand = CommandsManager.Create(() => SetValueInSelectedRows(y => y.Logo = null));

        }

        public ICommand ClearStartDateCommand { get; }

        public ICommand ClearEndDateCommand { get; }

        public ICommand ClearLogoCommand { get; }
    }
}

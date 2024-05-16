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
    internal class StadiumsImportViewModel : ImportViewModel<StadiumImportableViewModel>
    {
        private static readonly SampleFile SampleFile = new("import_stadiums_sample.xlsx", MyClubResources.ImportStadiumsSampleFileTitle, MyClubResources.ImportSampleFileScprojDescription, FileExtensionInfoProvider.Excel, ProcessHelper.OpenInExcel);

        public StadiumsImportViewModel(ProjectInfoProvider projectInfoProvider,
                                       DatabaseService databaseService,
                                       StadiumsImportService stadiumsImportService,
                                       StadiumService stadiumService)
            : base(new ImportSelectSourceViewModel<StadiumImportableViewModel>(new Dictionary<ImportSource, ImportSourceViewModel>
                                                {
                                                    { ImportSource.Database, new ImportSourceDatabaseViewModel<StadiumImportableViewModel>(new StadiumsDatabaseProvider(databaseService, stadiumService)) },
                                                    { ImportSource.File, new ImportSourceFileViewModel<StadiumImportableViewModel>(new StadiumsFileProvider(stadiumsImportService, stadiumService),
                                                                                                                                  ScprojFileExtensionInfo.Scproj.Concat(FileExtensionInfoProvider.Excel).Concat(FileExtensionInfoProvider.Csv, UiResources.AllFiles),
                                                                                                                                  SampleFile
                                                                                                                                  ) }
                                                }),
                  new StadiumsImportListParametersProvider())
            => projectInfoProvider.WhenProjectLoaded(x => Reset());
    }
}

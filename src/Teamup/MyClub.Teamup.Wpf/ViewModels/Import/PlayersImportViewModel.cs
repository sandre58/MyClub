// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Infrastructure.Packaging;
using MyClub.Teamup.Wpf.Collections;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.Resources;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.IO.FileExtensions;
using MyNet.Wpf.Schedulers;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class PlayersImportViewModel : ImportViewModel<PlayerImportableViewModel>
    {
        private static readonly SampleFile SampleFile = new("import_players_sample.xlsx", MyClubResources.ImportPlayersSampleFileTitle, MyClubResources.ImportSampleFileTmprojDescription, FileExtensionInfoProvider.Excel, ProcessHelper.OpenInExcel);

        public PlayersImportViewModel(
            ProjectInfoProvider projectInfoProvider,
            DatabaseService databaseService,
            PlayersImportService playersImportService,
            PlayerService playerService) : base(
                projectInfoProvider,
                new ImportSelectSourceViewModel<PlayerImportableViewModel>(new Dictionary<ImportSource, ImportSourceViewModel>
                                                {
                                                    { ImportSource.Database, new ImportSourceDatabaseViewModel<PlayerImportableViewModel>(new PlayersDatabaseProvider(databaseService, playerService)) },
                                                    { ImportSource.File, new ImportSourceFileViewModel<PlayerImportableViewModel>(new PlayersFileProvider(playersImportService, playerService),
                                                                                                                                  TmprojFileExtensionInfo.Tmproj.Concat(FileExtensionInfoProvider.Excel).Concat(FileExtensionInfoProvider.Csv, UiResources.AllFiles),
                                                                                                                                  SampleFile
                                                                                                                                  ) }
                                                }),
                new PlayersImportListParametersProvider())
        {
            SetImportInjuriesCommand = CommandsManager.Create<bool>(x => SetValueInSelectedRows(y => y.ImportInjuries = x));
            SetGenderCommand = CommandsManager.Create<GenderType>(x => SetValueInSelectedRows(y => y.Gender = x));
            SetCountryCommand = CommandsManager.Create<Country>(x => SetValueInSelectedRows(y => y.Country = x));
            SetCategoryCommand = CommandsManager.Create<Category>(x => SetValueInSelectedRows(y => y.Category = x));
            SetTeamCommand = CommandsManager.Create<TeamViewModel>(x => SetValueInSelectedRows(y => y.Team = x));
            SetLicenseStateCommand = CommandsManager.Create<LicenseState>(x => SetValueInSelectedRows(y => y.LicenseState = x));
            SetLateralityCommand = CommandsManager.Create<Laterality>(x => SetValueInSelectedRows(y => y.Laterality = x));
            ClearNumberCommand = CommandsManager.Create(() => SetValueInSelectedRows(y => y.Number.Value = null));
            ClearFromDateCommand = CommandsManager.Create(() => SetValueInSelectedRows(y => y.FromDate = null));
            ClearSizeCommand = CommandsManager.Create(() => SetValueInSelectedRows(y => y.Size = null));
            ClearShoesSizeCommand = CommandsManager.Create(() => SetValueInSelectedRows(y => y.ShoesSize.Value = null));
            ClearPhotoCommand = CommandsManager.Create(() => SetValueInSelectedRows(y => y.Photo = null));

            var teamsWithNullValue = new ObservableCollectionExtended<TeamViewModel>();

            TeamsWithNullValue = new(teamsWithNullValue);

            Disposables.Add(TeamsCollection.MyTeams.Connect().ObserveOn(WpfScheduler.Current).Bind(teamsWithNullValue).Subscribe());

            teamsWithNullValue.Insert(0, null!);
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public ReadOnlyObservableCollection<TeamViewModel> TeamsWithNullValue { get; }

        public ICommand SetImportInjuriesCommand { get; }

        public ICommand SetGenderCommand { get; }

        public ICommand SetCountryCommand { get; }

        public ICommand SetCategoryCommand { get; }

        public ICommand SetTeamCommand { get; }

        public ICommand SetLicenseStateCommand { get; }

        public ICommand SetLateralityCommand { get; }

        public ICommand ClearNumberCommand { get; }

        public ICommand ClearSizeCommand { get; }

        public ICommand ClearShoesSizeCommand { get; }

        public ICommand ClearFromDateCommand { get; }

        public ICommand ClearPhotoCommand { get; }
    }
}

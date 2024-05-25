// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using DynamicData.Binding;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Wpf.Collections;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities.Geography;
using MyNet.Utilities;
using MyNet.Wpf.Schedulers;
using System.Reactive.Linq;
using DynamicData;
using System;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class PlayersImportBySourcesDialogViewModel : ImportBySourcesDialogViewModel<PlayerImportableViewModel, PlayersImportListViewModel>
    {
        public PlayersImportBySourcesDialogViewModel(ProjectInfoProvider projectInfoProvider, PlayersImportBySourcesProvider provider)
            : base(provider, new PlayersImportListViewModel(provider))
            => projectInfoProvider.WhenProjectChanging(_ => Reset());
    }

    internal class PlayersImportListViewModel : ImportListViewModel<PlayerImportableViewModel>
    {
        public PlayersImportListViewModel(PlayersImportBySourcesProvider provider)
            : base(provider, new PlayerImportablesListParametersProvider())
        {
            SetImportInjuriesCommand = CommandsManager.Create<bool>(x => ApplyOnSelection(y => y.ImportInjuries = x));
            SetGenderCommand = CommandsManager.Create<GenderType>(x => ApplyOnSelection(y => y.Gender = x));
            SetCountryCommand = CommandsManager.Create<Country>(x => ApplyOnSelection(y => y.Country = x));
            SetCategoryCommand = CommandsManager.Create<Category>(x => ApplyOnSelection(y => y.Category = x));
            SetTeamCommand = CommandsManager.Create<TeamViewModel>(x => ApplyOnSelection(y => y.Team = x));
            SetLicenseStateCommand = CommandsManager.Create<LicenseState>(x => ApplyOnSelection(y => y.LicenseState = x));
            SetLateralityCommand = CommandsManager.Create<Laterality>(x => ApplyOnSelection(y => y.Laterality = x));
            ClearNumberCommand = CommandsManager.Create(() => ApplyOnSelection(y => y.Number.Value = null));
            ClearFromDateCommand = CommandsManager.Create(() => ApplyOnSelection(y => y.FromDate = null));
            ClearSizeCommand = CommandsManager.Create(() => ApplyOnSelection(y => y.Size = null));
            ClearShoesSizeCommand = CommandsManager.Create(() => ApplyOnSelection(y => y.ShoesSize.Value = null));
            ClearPhotoCommand = CommandsManager.Create(() => ApplyOnSelection(y => y.Photo = null));

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

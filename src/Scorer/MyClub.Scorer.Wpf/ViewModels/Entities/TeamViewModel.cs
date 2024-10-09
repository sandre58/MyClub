// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.Commands;
using MyNet.UI.Threading;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Wpf.Extensions;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class TeamViewModel : EntityViewModelBase<Team>, IEditableTeamViewModel, IVirtualTeamViewModel
    {
        private readonly TeamPresentationService _teamPresentationService;
        private readonly StadiumsProvider _stadiumsProvider;
        private readonly ObservableCollectionExtended<PlayerViewModel> _players = [];
        private readonly ObservableCollectionExtended<ManagerViewModel> _staff = [];

        public TeamViewModel(Team item, TeamPresentationService teamPresentationService, PersonPresentationService personPresentationService, StadiumsProvider stadiumsProvider) : base(item)
        {
            _stadiumsProvider = stadiumsProvider;
            _teamPresentationService = teamPresentationService;

            Players = new(_players);
            Staff = new(_staff);

            OpenCommand = CommandsManager.Create(async () => await OpenAsync().ConfigureAwait(false));
            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                Item.Players.ToObservableChangeSet().Transform(x => new PlayerViewModel(x, this, personPresentationService)).ObserveOn(Scheduler.UI).Bind(_players).DisposeMany().Subscribe(),
                Item.Staff.ToObservableChangeSet().Transform(x => new ManagerViewModel(x, this, personPresentationService)).ObserveOn(Scheduler.UI).Bind(_staff).DisposeMany().Subscribe(),
            ]);
        }

        public string Name => Item.Name;

        public string ShortName => Item.ShortName;

        public byte[]? Logo => Item.Logo;

        public Color? HomeColor => Item.HomeColor.ToColor();

        public Color? AwayColor => Item.AwayColor.ToColor();

        public Country? Country => Item.Country;

        public StadiumViewModel? Stadium => Item.Stadium is not null ? _stadiumsProvider.Get(Item.Stadium.Id) : null;

        IEditableStadiumViewModel? IEditableTeamViewModel.Stadium => Stadium;

        public ReadOnlyObservableCollection<PlayerViewModel> Players { get; }

        public ReadOnlyObservableCollection<ManagerViewModel> Staff { get; }

        public ICommand EditCommand { get; }

        public ICommand OpenCommand { get; }

        public async Task EditAsync() => await _teamPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task OpenAsync() => await _teamPresentationService.OpenAsync(this).ConfigureAwait(false);

        public override string ToString() => Name;
    }
}

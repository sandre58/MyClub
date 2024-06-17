// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Wpf.Extensions;
using MyNet.Utilities;
using MyNet.UI.Threading;
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.TacticPage
{
    internal class TacticDetailsViewModel : ItemViewModel<TacticViewModel>
    {
        private readonly PlayersProvider _playersProvider;
        private readonly ProjectInfoProvider _projectInfoProvider;

        public Color? HomeColor { get; private set; }

        public TacticDetailsViewModel(ProjectInfoProvider projectInfoProvider, PlayersProvider playersProvider)
        {
            _playersProvider = playersProvider;
            _projectInfoProvider = projectInfoProvider;
            Disposables.AddRange([
                _playersProvider.Connect().WhenAnyPropertyChanged(nameof(PlayerViewModel.TeamId), nameof(PlayerViewModel.Positions)).ObserveOn(Scheduler.UI).Subscribe(_ => RefreshPlayers()),
                _playersProvider.Connect().ObserveOn(Scheduler.UI).Subscribe(_ => RefreshPlayers())
            ]);

            projectInfoProvider.WhenClubPropertyChanged(x => x.HomeColor, x => HomeColor = x.ToColor());
            projectInfoProvider.WhenProjectChanged(x => HomeColor = x?.Club.HomeColor.ToColor());
        }

        public TacticPositionViewModel? SelectedPosition { get; set; }

        public ObservableCollection<RatedPositionViewModel> BestPlayerPositionsInDefaultTeams { get; private set; } = [];

        public ObservableCollection<RatedPositionViewModel> BestPlayerPositionsInOtherTeams { get; private set; } = [];

        protected virtual void OnSelectedPositionChanged() => RefreshPlayers();

        private void RefreshPlayers()
        {
            if (SelectedPosition is null)
            {
                BestPlayerPositionsInDefaultTeams.Clear();
                BestPlayerPositionsInOtherTeams.Clear();
            }
            else
            {
                var bestPositions = _playersProvider.Items.SelectMany(x => x.Positions)
                                                          .Where(x => x.Position.IsSimilar(SelectedPosition.Position))
                                                          .OrderByDescending(x => x.Rating)
                                                          .ThenByDescending(x => x.IsNatural)
                                                          .ThenBy(x => x.Player.FullName);

                var mainTeamIds = _projectInfoProvider.GetCurrentProject().OrThrow().GetMainTeams().Select(y => y.Id).ToList();
                var bestPositionsInDefaultTeams = bestPositions.Where(x => x.Player.TeamId is not null && mainTeamIds.Contains(x.Player.TeamId));
                var bestPositionsInOtherTeams = bestPositions.Where(x => x.Player.TeamId is null || !mainTeamIds.Contains(x.Player.TeamId));

                BestPlayerPositionsInDefaultTeams.Set(bestPositionsInDefaultTeams);
                BestPlayerPositionsInOtherTeams.Set(bestPositionsInOtherTeams);
            }
        }
    }
}

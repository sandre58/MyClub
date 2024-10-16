// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.Observable.Deferrers;
using MyNet.UI.Collections;
using MyNet.UI.Extensions;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.StatisticsPage
{
    internal class StatisticsPageViewModel : PageViewModel
    {
        private readonly TeamsProvider _teamsProvider;
        private readonly MatchStatisticsService _matchStatisticsService;
        private readonly MatchesProvider _matchesProvider;
        private readonly SingleTaskDeferrer _refreshDeferrer;
        private readonly UiObservableCollection<PlayerStatisticsViewModel> _scorers = [];

        public StatisticsPageViewModel(MatchesProvider matchesProvider, MatchStatisticsService matchStatisticsService, TeamsProvider teamsProvider)
        {
            _matchesProvider = matchesProvider;
            _matchStatisticsService = matchStatisticsService;
            _teamsProvider = teamsProvider;
            Scorers = new(_scorers);
            _refreshDeferrer = new(async x => await RefreshAllAsync(x).ConfigureAwait(false), throttle: 100);

            //Disposables.AddRange(
            //    [
            //        _matchesProvider.Connect().Subscribe(_ => _refreshDeferrer.AskRefresh())
            //    ]);
        }

        public ReadOnlyObservableCollection<PlayerStatisticsViewModel> Scorers { get; }

        private async Task RefreshAllAsync(CancellationToken token)
            => await BusyService.WaitIndeterminateAsync(() =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    var statistics = _matchStatisticsService.GetPlayerStatistics();
                    _scorers.Set(statistics.Where(x => x.Goals > 0).Select(x => new PlayerStatisticsViewModel(_teamsProvider.Items.OfType<TeamViewModel>().SelectMany(y => y.Players).GetById(x.PlayerId),
                                                                                                              _teamsProvider.GetOrThrow(x.TeamId),
                                                                                                              x.Goals)));
                }
                catch (OperationCanceledException)
                {
                    // Noting
                }
            });
    }

    internal class PlayerStatisticsViewModel : Wrapper<PlayerViewModel>
    {
        public PlayerStatisticsViewModel(PlayerViewModel item, TeamViewModel team, int value) : base(item)
        {
            Team = team;
            Value = value;
        }

        public TeamViewModel Team { get; }

        public int Value { get; }
    }
}

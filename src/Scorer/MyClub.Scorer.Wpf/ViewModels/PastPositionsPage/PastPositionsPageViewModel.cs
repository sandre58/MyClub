// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Humanizer;
using MyNet.Observable;
using MyNet.UI.Collections;
using MyNet.UI.Extensions;
using MyNet.UI.Navigation.Models;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Wpf.ViewModels.PastPositionsPage
{
    internal class PastPositionsPageViewModel : PageViewModel
    {
        private readonly CompetitionInfoProvider _competitionInfoProvider;
        private readonly LeagueService _leagueService;
        private readonly UiObservableCollection<TeamPositionsSerieWrapper> _teamSeries = [];
        private CompositeDisposable? _leagueSubscriptions;

        public PastPositionsPageViewModel(CompetitionInfoProvider competitionInfoProvider,
                                          TeamsProvider teamsProvider,
                                          LeagueService leagueService)
        {
            _competitionInfoProvider = competitionInfoProvider;
            _leagueService = leagueService;
            TeamSeries = new(_teamSeries);

            Disposables.AddRange(
                [
                    teamsProvider.ConnectById()
                                 .Transform(x => new TeamPositionsSerieWrapper(x))
                                 .AutoRefresh(x => x.Rank)
                                 .SortAndBind(_teamSeries, SortExpressionComparer<TeamPositionsSerieWrapper>.Ascending(x => x.Rank))
                                 .Subscribe(),
                    TeamSeries.ToObservableChangeSet()
                              .Transform(x => x.Serie)
                              .OnItemAdded(Series.Add)
                              .OnItemRemoved(x => Series.Remove(x))
                              .Subscribe(_ => RefreshYLabels())
                ]);

            competitionInfoProvider.WhenCompetitionChanged(async x =>
            {
                if (x is LeagueViewModel leagueViewModel)
                {
                    _leagueSubscriptions = new(leagueViewModel.WhenRankingChanged(async () => await RefreshAllAsync().ConfigureAwait(false)));
                    await RefreshAllAsync().ConfigureAwait(false);
                }
            }, _ =>
            {
                _leagueSubscriptions?.Dispose();
                _leagueSubscriptions = null;
            });
        }

        public List<string>? AxeXLabels { get; private set; }

        public List<string>? AxeYLabels { get; private set; }

        public ReadOnlyObservableCollection<TeamPositionsSerieWrapper> TeamSeries { get; }

        public SeriesCollection Series { get; } = [];

        private async Task RefreshAllAsync()
            => await BusyService.WaitIndeterminateAsync(() =>
            {
                var league = _competitionInfoProvider.GetCompetition<LeagueViewModel>();
                var matchdays = league.Matchdays.OrderBy(x => x.OriginDate).ToList();
                var rankingByMatchdays = matchdays.Select(x => x.Matches.Any(y => y.IsPlayed)
                                                          ? (x, _leagueService.GetRanking(x.Id))
                                                          : ((MatchdayViewModel, RankingDto)?)null).ToList();

                AxeXLabels = matchdays.Select(x => x.ShortName).ToList();

                TeamSeries.ToList().ForEach(x => x.Update(league.Ranking.GetRow(x.Team)?.Rank ?? 0,
                                                 rankingByMatchdays.Select(y =>
                                                 {
                                                     var row = y?.Item2.Rows?.Find(z => z.TeamId == x.Team.Id);
                                                     return new PastPosition(row?.Rank,
                                                                             row is not null ? TeamSeries.Count - row.Rank : null,
                                                                             y?.Item1.Matches.FirstOrDefault(z => z.Participate(x.Team)));
                                                 }).ToList()));
            });

        private void RefreshYLabels() => AxeYLabels = EnumerableHelper.Range(TeamSeries.Count, 1, -1).Select(x => x.Ordinalize().OrEmpty()).ToList();

        public override void LoadParameters(INavigationParameters? parameters)
        {
            base.LoadParameters(parameters);

            if (parameters?.Get<object>(NavigationCommandsService.SelectionParameterKey) is IEnumerable<Guid> teamIds)
                TeamSeries.ForEach(x => x.IsVisible = teamIds.Contains(x.Team.Id));
        }
        protected override void OnCultureChanged()
        {
            base.OnCultureChanged();
            RefreshYLabels();
        }
    }

    internal class TeamPositionsSerieWrapper : Wrapper<TeamViewModel>
    {
        private readonly ObservableCollection<PastPosition> _pastPositions = [];

        public bool IsVisible { get; set; }

        public LineSeries Serie { get; }

        public int Rank { get; private set; }

        public TeamViewModel Team { get; private set; }

        public TeamPositionsSerieWrapper(TeamViewModel team) : base(team)
        {
            Team = team;
            Serie = new LineSeries(GetMapper())
            {
                Values = new ChartValues<PastPosition>(),
                Fill = Brushes.Transparent,
                LineSmoothness = 0,
                PointGeometry = DefaultGeometries.Diamond,
                Title = team.Name,
                Stroke = new SolidColorBrush(Item.HomeColor.GetValueOrDefault()),
                PointGeometrySize = 8,
            };

        }

        private static CartesianMapper<PastPosition> GetMapper()
            => Mappers.Xy<PastPosition>()
                      .X((_, index) => index)
                      .Y(row => row.InverseRank ?? double.NaN);

        protected virtual void OnIsVisibleChanged()
        {
            if (!IsVisible && Serie.Values.Count > 0)
                Serie.Values.Clear();
            else if (IsVisible && Serie.Values.Count == 0)
                Serie.Values.AddRange(_pastPositions);
        }

        public void Update(int rank, IEnumerable<PastPosition> pastPositions)
        {
            Rank = rank;
            _pastPositions.Set(pastPositions);

            if (IsVisible)
            {
                Serie.Values.Clear();
                Serie.Values.AddRange(_pastPositions);
            }
        }
    }

    internal class PastPosition(int? rank, int? inverseRank, MatchViewModel? match)
    {
        public int? Rank { get; } = rank;

        public int? InverseRank { get; } = inverseRank;

        public MatchViewModel? Match { get; } = match;
    }
}

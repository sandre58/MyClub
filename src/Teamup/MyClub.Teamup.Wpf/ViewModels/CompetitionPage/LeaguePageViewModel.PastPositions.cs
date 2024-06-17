// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using LiveCharts;
using LiveCharts.Wpf;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;
using MyNet.Humanizer;
using MyNet.Observable;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.UI.Extensions;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class LeaguePagePastPositionsViewModel : SubItemViewModel<LeagueViewModel>
    {
        private readonly CompetitionService _competitionService;

        public LeaguePagePastPositionsViewModel(CompetitionService competitionService)
        {
            _competitionService = competitionService;

            Disposables.Add(TeamSeries.ToObservableChangeSet()
                                      .Transform(x => x.Serie)
                                      .OnItemAdded(Series.Add)
                                      .OnItemRemoved(x => Series.Remove(x))
                                      .Subscribe());
        }

        public List<string>? AxeXLabels { get; private set; }

        public List<string>? AxeYLabels { get; private set; }

        public ObservableCollection<TeamPositionsSerieWrapper> TeamSeries { get; } = [];

        public SeriesCollection Series { get; } = [];

        protected override async void OnItemChanged()
        {
            base.OnItemChanged();

            if (Item is null) return;

            ItemSubscriptions?.Add(Item.WhenRankingChanged(async () => await RefreshAllAsync().ConfigureAwait(false)));

            await RefreshAllAsync().ConfigureAwait(false);
        }

        private async Task RefreshAllAsync()
            => await BusyService.WaitIndeterminateAsync(() =>
            {
                if (Item is null) return;

                var matchdays = Item.Matchdays.OrderBy(x => x.OriginDate).ToList();
                var rankings = matchdays.Where(x => x.OriginDate.IsInPast()).Select(x => _competitionService.GetRankingByMatchday(Item.Id, x.Id)).ToList();

                AxeXLabels = matchdays.Select(x => x.ShortName).ToList();
                RefreshYLabels();
                RefreshSeries(Item.Ranking.Source.Select(x => x.Team).ToList(), rankings);
            });

        private void RefreshYLabels() => AxeYLabels = EnumerableHelper.Range(Item?.Teams.Count ?? 1, 1, -1).Select(x => x.Ordinalize().OrEmpty()).ToList();

        private void RefreshSeries(IEnumerable<TeamViewModel> teams, IEnumerable<Ranking> rankings)
            => MyNet.UI.Threading.Scheduler.UI.Schedule(() => TeamSeries.Set(teams.Select(x => new TeamPositionsSerieWrapper(x, rankings.Select(y => y.Count() - (double)y.GetRank(x.Id)))).ToList()));

        protected override void OnCultureChanged()
        {
            base.OnCultureChanged();
            RefreshYLabels();
        }
    }

    internal class TeamPositionsSerieWrapper : Wrapper<TeamViewModel>
    {
        public bool IsVisible { get; set; }

        public LineSeries Serie { get; }

        public TeamPositionsSerieWrapper(TeamViewModel team, IEnumerable<double> values) : base(team)
        {
            Serie = new LineSeries
            {
                Values = new ChartValues<double>(values),
                Fill = Brushes.Transparent,
                LineSmoothness = 0,
                PointGeometry = DefaultGeometries.Diamond,
                Title = team.Name,
            };
            IsVisible = team.IsMyTeam;
            UpdateSerieVisibility();
        }

        protected virtual void OnIsVisibleChanged() => UpdateSerieVisibility();

        private void UpdateSerieVisibility()
        {
            if (IsVisible)
            {
                Serie.Stroke = new SolidColorBrush(Item.HomeColor.GetValueOrDefault());
                Serie.PointGeometrySize = 8;
            }
            else
            {
                Serie.Stroke = Brushes.Transparent;
                Serie.PointGeometrySize = 0;
            }
        }
    }
}

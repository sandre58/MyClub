// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Domain.Enums;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.Commands;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.RankingPage
{
    internal class RankingPageViewModel : PageViewModel
    {
        private readonly RankingListParameterProvider _rankingListParameterProvider = new();
        private CompositeDisposable? _disposables;

        public RankingPageViewModel(ProjectInfoProvider projectInfoProvider, CompetitionInfoProvider competitionInfoProvider, MatchesProvider matchesProvider, LeaguePresentationService leaguePresentationService)
        {
            projectInfoProvider.WhenProjectClosing(() =>
            {
                Ranking?.Dispose();
                LiveRanking?.Dispose();
                HomeRanking?.Dispose();
                AwayRanking?.Dispose();

                Ranking = null;
                LiveRanking = null;
                HomeRanking = null;
                AwayRanking = null;
                _disposables?.Dispose();
            });

            projectInfoProvider.WhenProjectLoaded(_ =>
            {
                MyNet.UI.Threading.Scheduler.GetUIOrCurrent().Schedule(() =>
                {
                    if (competitionInfoProvider.GetCompetition() is LeagueViewModel leagueViewModel)
                    {
                        Ranking = new RankingListViewModel(leagueViewModel.Ranking, _rankingListParameterProvider);
                        LiveRanking = new RankingListViewModel(leagueViewModel.LiveRanking, _rankingListParameterProvider);
                        HomeRanking = new RankingListViewModel(leagueViewModel.HomeRanking, _rankingListParameterProvider);
                        AwayRanking = new RankingListViewModel(leagueViewModel.AwayRanking, _rankingListParameterProvider);

                        _disposables = new(leagueViewModel.SchedulingParameters.WhenPropertyChanged(x => x.UseTeamVenues).Subscribe(x => ShowHomeAwayRankings = x.Value));
                    }
                });
            });

            EditRulesCommand = CommandsManager.Create(async () => await leaguePresentationService.EditRankingRulesAsync().ConfigureAwait(false));

            Disposables.Add(matchesProvider.Connect().AutoRefresh(x => x.State).Throttle(50.Milliseconds()).Subscribe(_ => LiveIsEnabled = matchesProvider.Items.Any(x => x.State == MatchState.InProgress)));
        }

        public bool LiveIsEnabled { get; private set; }

        public RankingListViewModel? Ranking { get; private set; }

        public RankingListViewModel? LiveRanking { get; private set; }

        public RankingListViewModel? HomeRanking { get; private set; }

        public RankingListViewModel? AwayRanking { get; private set; }

        public ICommand EditRulesCommand { get; private set; }

        public bool ShowHomeAwayRankings { get; set; }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.Localization;

namespace MyClub.Scorer.Wpf.ViewModels.HomePage.DashboardContent
{
    internal class DashboardViewModel : ObservableObject
    {
        public DashboardViewModel(ProjectInfoProvider projectInfoProvider,
                                  CompetitionInfoProvider competitionInfoProvider,
                                  MatchesProvider matchesProvider,
                                  TeamsProvider teamsProvider,
                                  StadiumsProvider stadiumsProvider)
        {
            CalendarViewModel = new(matchesProvider);
            RankingViewModel = new(competitionInfoProvider);
            PreviousMatchesViewModel = new(matchesProvider.Connect()
                                                 .AutoRefresh(x => x.Date)
                                                 .Filter(x => x.Date.IsBetween(GlobalizationService.Current.Date.Add(-projectInfoProvider.PeriodForPreviousMatches), GlobalizationService.Current.Date))
                                                 .Sort(SortExpressionComparer<MatchViewModel>.Ascending(x => x.Date)));
            NextMatchesViewModel = new(matchesProvider.Connect()
                                     .AutoRefresh(x => x.Date)
                                     .Filter(x => x.Date.IsBetween(GlobalizationService.Current.Date, GlobalizationService.Current.Date.Add(projectInfoProvider.PeriodForPreviousMatches)))
                                     .Sort(SortExpressionComparer<MatchViewModel>.Ascending(x => x.Date)));
            LiveMatchesViewModel = new(matchesProvider.Connect()
                         .AutoRefresh(x => x.State)
                         .Filter(x => x.State == MyClub.Domain.Enums.MatchState.InProgress)
                         .Sort(SortExpressionComparer<MatchViewModel>.Ascending(x => x.Date)));

            Disposables.AddRange(
            [
                projectInfoProvider.WhenPropertyChanged(x => x.Name).Subscribe(x => Name = x.Value),
                projectInfoProvider.WhenPropertyChanged(x => x.Image).Subscribe(x => Image = x.Value),
                teamsProvider.Connect().Subscribe(_ => CountTeams = teamsProvider.Count),
                stadiumsProvider.Connect().Subscribe(_ => CountStadiums = stadiumsProvider.Count),
            ]);
        }

        public string? Name { get; private set; }

        public byte[]? Image { get; private set; }

        public int CountTeams { get; private set; }

        public int CountStadiums { get; set; }

        public OverviewCalendarViewModel CalendarViewModel { get; }

        public OverviewRankingViewModel RankingViewModel { get; }

        public ListViewModel<MatchViewModel> PreviousMatchesViewModel { get; }

        public ListViewModel<MatchViewModel> NextMatchesViewModel { get; }

        public ListViewModel<MatchViewModel> LiveMatchesViewModel { get; }

        protected override void Cleanup()
        {
            base.Cleanup();
            CalendarViewModel.Dispose();
            RankingViewModel.Dispose();
        }
    }
}

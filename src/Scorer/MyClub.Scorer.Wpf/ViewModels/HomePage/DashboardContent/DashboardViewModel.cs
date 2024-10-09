// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.SchedulePage;
using MyNet.Observable;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.UI.ViewModels.List.Paging;
using MyNet.Utilities;
using MyNet.Utilities.Comparaison;
using MyNet.Utilities.Localization;

namespace MyClub.Scorer.Wpf.ViewModels.HomePage.DashboardContent
{
    internal class DashboardViewModel : ObservableObject
    {
        private readonly ProjectInfoProvider _projectInfoProvider;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly MatchesProvider _matchesProvider;
        private readonly ReadOnlyObservableCollection<MatchViewModel> _pendingMatches;
        private readonly ReadOnlyObservableCollection<MatchViewModel> _lastMatches;
        private readonly ReadOnlyObservableCollection<MatchViewModel> _nextMatches;
        private readonly ReadOnlyObservableCollection<MatchViewModel> _liveMatches;
        private readonly ReadOnlyObservableCollection<MatchViewModel> _postponedMatches;
        private readonly ReadOnlyObservableCollection<MatchViewModel> _conflictMatches;

        public DashboardViewModel(ProjectInfoProvider projectInfoProvider,
                                  CompetitionInfoProvider competitionInfoProvider,
                                  MatchesProvider matchesProvider,
                                  MatchPresentationService matchPresentationService)
        {
            _projectInfoProvider = projectInfoProvider;
            _matchesProvider = matchesProvider;
            _matchPresentationService = matchPresentationService;

            CalendarViewModel = new(matchesProvider);
            RankingViewModel = new(competitionInfoProvider);

            OpenSchedulingAssistantCommand = CommandsManager.Create(async () => await OpenSchedulingAssistantAsync().ConfigureAwait(false));
            NavigateToPendingMatchesCommand = CommandsManager.Create(() => NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeList), new List<IFilterViewModel>
                        {
                            new EnumValuesFilterViewModel<MatchState>(nameof(MatchViewModel.State)) { Values = new List<MatchState> { MatchState.None } },
                            new DateFilterViewModel(nameof(MatchViewModel.Date), ComplexComparableOperator.IsBetween, null, GlobalizationService.Current.Date)
                        }));
            NavigateToLastMatchesCommand = CommandsManager.Create(() =>
            {
                var stages = matchesProvider.Items.Select(x => x.Stage).OrderBy(x => x).ToList();
                var stage = stages.LastOrDefault(x => x.Date.IsInPast()) ?? stages.FirstOrDefault();
                NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeByStage), stage?.Id);
            });
            NavigateToNextMatchesCommand = CommandsManager.Create(() =>
            {
                var stages = matchesProvider.Items.Select(x => x.Stage).OrderBy(x => x).ToList();
                var stage = stages.FirstOrDefault(x => x.Date.IsInFuture()) ?? stages.LastOrDefault();
                NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeByStage), stage?.Id);
            });
            NavigateToPostponedMatchesCommand = CommandsManager.Create(() => NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeList), new List<IFilterViewModel>
                        {
                            new EnumValuesFilterViewModel<MatchState>(nameof(MatchViewModel.State)) { Values = new List<MatchState> { MatchState.Postponed } },
                        }));
            NavigateToLiveMatchesCommand = CommandsManager.Create(() => NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeList), new List<IFilterViewModel>
                        {
                            new EnumValuesFilterViewModel<MatchState>(nameof(MatchViewModel.State)) { Values = new List<MatchState> { MatchState.InProgress, MatchState.Suspended } },
                        }));

            Disposables.AddRange(
            [
                projectInfoProvider.WhenPropertyChanged(x => x.Name).Subscribe(x => Name = x.Value),
                projectInfoProvider.WhenPropertyChanged(x => x.Image).Subscribe(x => Image = x.Value),
                competitionInfoProvider.WhenPropertyChanged(x => x.StartDate).Subscribe(x => StartDate = x.Value),
                competitionInfoProvider.WhenPropertyChanged(x => x.EndDate).Subscribe(x => EndDate = x.Value),
                competitionInfoProvider.WhenPropertyChanged(x => x.State).Subscribe(x => State = x.Value ?? CompetitionState.Incoming),
                competitionInfoProvider.WhenPropertyChanged(x => x.Type).Subscribe(x =>
                {
                    IsLeague = x.Value is CompetitionType.League;
                    IsCup = x.Value is CompetitionType.Cup;
                }),
                matchesProvider.Connect()
                               .AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(nameof(MatchViewModel.Date), nameof(MatchViewModel.State)))
                               .Filter(x => x.State == MatchState.None && x.EndDate.IsInPast())
                               .Bind(out _pendingMatches)
                               .Subscribe(),
                matchesProvider.Connect()
                               .AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(nameof(MatchViewModel.Date), nameof(MatchViewModel.IsPlayed)))
                               .AutoRefreshOnObservable(_ => matchesProvider.LoadRunner.WhenEnd())
                               .AutoRefreshOnObservable(_ => _projectInfoProvider.Preferences.WhenAnyPropertyChanged(nameof(ProjectPreferencesViewModel.ShowLastMatchFallback), nameof(ProjectPreferencesViewModel.PeriodForPreviousMatches)))
                               .Filter(x => !matchesProvider.LoadRunner.IsRunning && x.IsPlayed
                                            && (x.Date.IsBetween(GlobalizationService.Current.Date.Add(-_projectInfoProvider.Preferences.PeriodForPreviousMatches), GlobalizationService.Current.Date)
                                                || (_projectInfoProvider.Preferences.ShowLastMatchFallback && x.Date == matchesProvider.Items.Where(y => y.State is MatchState.Played or MatchState.Suspended && y.Date.IsInPast()).MaxOrDefault(y => y.Date))))
                               .Bind(out _lastMatches)
                               .Subscribe(),
                matchesProvider.Connect()
                               .AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(nameof(MatchViewModel.Date), nameof(MatchViewModel.IsPlaying)))
                               .AutoRefreshOnObservable(_ => matchesProvider.LoadRunner.WhenEnd())
                               .AutoRefreshOnObservable(x => _projectInfoProvider.Preferences.WhenAnyPropertyChanged(nameof(ProjectPreferencesViewModel.ShowNextMatchFallback), nameof(ProjectPreferencesViewModel.PeriodForNextMatches)))
                               .Filter(x => !matchesProvider.LoadRunner.IsRunning && !x.IsPlaying
                                            && (x.EndDate.IsBetween(GlobalizationService.Current.Date, GlobalizationService.Current.Date.Add(_projectInfoProvider.Preferences.PeriodForNextMatches))
                                                || (_projectInfoProvider.Preferences.ShowNextMatchFallback && x.Date == matchesProvider.Items.Where(y => y.State != MatchState.InProgress && y.Date.IsInFuture()).MinOrDefault(y => y.Date))))
                               .Bind(out _nextMatches)
                               .Subscribe(),
                matchesProvider.Connect()
                               .AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(nameof(MatchViewModel.IsPlaying)))
                               .Filter(x => x.IsPlaying)
                               .Bind(out _liveMatches)
                               .Subscribe(),
                matchesProvider.Connect()
                               .AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(nameof(MatchViewModel.IsPostponed)))
                                .Filter(x => x.State == MatchState.Postponed)
                               .Bind(out _postponedMatches)
                               .Subscribe(),
                matchesProvider.Connect()
                               .AutoRefreshOnObservable(x => x.Conflicts.ToObservableChangeSet())
                               .Filter(x => x.Conflicts.Any())
                               .Bind(out _conflictMatches)
                               .Subscribe()
            ]);

            PendingMatches = new(_pendingMatches.ToObservableChangeSet(), matchPresentationService, new MatchesListParametersProvider(ListSortDirection.Ascending));
            LastMatches = new(_lastMatches.ToObservableChangeSet(), matchPresentationService, new MatchesListParametersProvider(ListSortDirection.Descending));
            NextMatches = new(_nextMatches.ToObservableChangeSet(), matchPresentationService, new MatchesListParametersProvider(ListSortDirection.Ascending));
            LiveMatches = new(_liveMatches.ToObservableChangeSet(), matchPresentationService, new MatchesListParametersProvider(ListSortDirection.Ascending));
            PostponedMatches = new(_postponedMatches.ToObservableChangeSet(), matchPresentationService, new MatchesListParametersProvider(ListSortDirection.Ascending));
            ConflictsMatches = new(_conflictMatches.ToObservableChangeSet(), matchPresentationService, new MatchesListParametersProvider(ListSortDirection.Ascending));

            matchesProvider.LoadRunner.Register(this,
                [
                    () => LastMatches.DeferRefresh(),
                    () => LiveMatches.DeferRefresh(),
                    () => NextMatches.DeferRefresh(),
                    () => PostponedMatches.DeferRefresh(),
                    () => ConflictsMatches.DeferRefresh(),
                    () => PendingMatches.DeferRefresh(),
                ]);
        }

        public string? Name { get; private set; }

        public byte[]? Image { get; private set; }

        public DateOnly? StartDate { get; private set; }

        public DateOnly? EndDate { get; private set; }

        public CompetitionState State { get; private set; }

        public bool IsLeague { get; private set; }

        public bool IsCup { get; private set; }

        public OverviewCalendarViewModel CalendarViewModel { get; }

        public OverviewRankingViewModel RankingViewModel { get; }

        public MatchesViewModel LastMatches { get; }

        public MatchesViewModel NextMatches { get; }

        public MatchesViewModel LiveMatches { get; }

        public MatchesViewModel PostponedMatches { get; }

        public MatchesViewModel ConflictsMatches { get; }

        public MatchesViewModel PendingMatches { get; }

        public ICommand OpenSchedulingAssistantCommand { get; }

        public ICommand NavigateToPendingMatchesCommand { get; }

        public ICommand NavigateToLastMatchesCommand { get; }

        public ICommand NavigateToNextMatchesCommand { get; }

        public ICommand NavigateToPostponedMatchesCommand { get; }

        public ICommand NavigateToLiveMatchesCommand { get; }

        private async Task OpenSchedulingAssistantAsync()
            => await _matchPresentationService.OpenSchedulingAssistantAsync(_matchesProvider.Items).ConfigureAwait(false);

        protected override void Cleanup()
        {
            base.Cleanup();
            _matchesProvider.LoadRunner.Unregister(this);
            CalendarViewModel.Dispose();
            RankingViewModel.Dispose();
            LastMatches.Dispose();
            NextMatches.Dispose();
            LiveMatches.Dispose();
            PostponedMatches.Dispose();
            ConflictsMatches.Dispose();
            PendingMatches.Dispose();
        }
    }

    internal sealed class MatchesListParametersProvider : ListParametersProvider
    {
        public MatchesListParametersProvider(ListSortDirection sortDirection) : base(nameof(MatchViewModel.Date), sortDirection) { }

        public override IPagingViewModel ProvidePaging() => new PagingViewModel(10);
    }
}

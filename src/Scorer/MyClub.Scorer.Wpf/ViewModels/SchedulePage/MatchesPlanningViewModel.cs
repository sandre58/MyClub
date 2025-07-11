﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Wpf.Filters;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.Observable.Translatables;
using MyNet.UI.Commands;
using MyNet.UI.Selection;
using MyNet.UI.Selection.Models;
using MyNet.UI.Threading;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.Utilities;
using MyNet.Utilities.Localization;
using PropertyChanged;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal class MatchesPlanningViewModel : MatchesViewModel
    {
        private readonly MatchesProvider _matchesProvider;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly AvailibilityCheckingService _availibilityCheckingService;

        public MatchesPlanningViewModel(SchedulingParametersViewModel schedulingParameters,
                                        MatchesProvider matchesProvider,
                                        CompetitionStagesProvider competitionStagesProvider,
                                        TeamsProvider teamsProvider,
                                        StadiumsProvider stadiumsProvider,
                                        MatchPresentationService matchPresentationService,
                                        AvailibilityCheckingService availibilityCheckingService)
            : base(new MatchesCollection(matchesProvider), matchPresentationService,
                   new MatchesPlanningListParametersProvider(competitionStagesProvider.Items,
                                                             new ObservableSourceProvider<DateOnly>(matchesProvider.Connect()
                                                                                                                   .AutoRefreshOnObservable(x => Observable.FromEventPattern(x => GlobalizationService.Current.TimeZoneChanged += x, x => GlobalizationService.Current.TimeZoneChanged -= x))
                                                                                                                   .AutoRefreshOnObservable(x => x.WhenPropertyChanged(y => y.DateOfDay))
                                                                                                                   .DistinctValues(x => x.DateOfDay)
                                                                                                                   .ObserveOn(Scheduler.UI)).Source,
                                                             teamsProvider.Items,
                                                             stadiumsProvider.Items,
                                                             schedulingParameters))
        {
            _matchesProvider = matchesProvider;
            _matchPresentationService = matchPresentationService;
            _availibilityCheckingService = availibilityCheckingService;
            Stadiums = new ListViewModel<IStadiumWrapper>(stadiumsProvider.Connect().Transform(x => (IStadiumWrapper)new StadiumWrapper(x)).Merge(new ObservableCollection<IStadiumWrapper>() { new AutomaticStadiumWrapper(), new NoStadiumWrapper() }.ToObservableChangeSet()));

            SelectAllByStageCommand = CommandsManager.CreateNotNull<IStageViewModel>(x => Collection.Select(Items.Where(y => y.Stage == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.Stage == x && y.IsSelectable).Any(y => !y.IsSelected));
            UnselectAllByStageCommand = CommandsManager.CreateNotNull<IStageViewModel>(x => Collection.Unselect(Items.Where(y => y.Stage == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.Stage == x && y.IsSelectable).All(y => y.IsSelected));
            SelectAllByDateCommand = CommandsManager.CreateNotNull<DateOnly>(x => Collection.Select(Items.Where(y => y.DateOfDay == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.DateOfDay == x && y.IsSelectable).Any(y => !y.IsSelected));
            UnselectAllByDateCommand = CommandsManager.CreateNotNull<DateOnly>(x => Collection.Unselect(Items.Where(y => y.DateOfDay == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.DateOfDay == x && y.IsSelectable).All(y => y.IsSelected));
            OpenSchedulingAssistantCommand = CommandsManager.Create(async () => await OpenSchedulingAssistantAsync().ConfigureAwait(false));
            RescheduleConflictsCommand = CommandsManager.CreateNotNull<MatchViewModel>(async x => await RescheduleConflictsAsync(x).ConfigureAwait(false), x => x.MatchesInConflicts.Count > 0);
            EditResultsCommand = CommandsManager.Create(StartEditResults, () => Mode == ScreenMode.Read && Items.Count > 0);
            ValidateResultsCommand = CommandsManager.Create(async () => await ValidateResultsAsync().ConfigureAwait(false), () => Mode == ScreenMode.Edition);
            CancelResultsCommand = CommandsManager.Create(CancelResults, () => Mode == ScreenMode.Edition);
            SelectConflictsCommand = CommandsManager.CreateNotNull<MatchViewModel>(SelectConflicts, x => x.MatchesInConflicts.Count > 0);
            SelectFixtureMatchesCommand = CommandsManager.CreateNotNull<MatchViewModel>(SelectFixtureMatches, x => x.Fixture?.Matches.Count > 0);

            Disposables.AddRange(
            [
                SelectedWrappers.ToObservableChangeSet().Throttle(1000.Milliseconds()).Subscribe(_ => ValidateStadiumsAvaibility(SelectedItems.ToList())),
            ]);

            matchesProvider.LoadRunner.Register(this, DeferRefresh);
        }

        [CanBeValidated]
        [CanSetIsModified]
        public ListViewModel<IStadiumWrapper> Stadiums { get; }

        public ICommand SelectAllByStageCommand { get; }

        public ICommand UnselectAllByStageCommand { get; }

        public ICommand SelectAllByDateCommand { get; }

        public ICommand UnselectAllByDateCommand { get; }

        public ICommand OpenSchedulingAssistantCommand { get; }

        public ICommand RescheduleConflictsCommand { get; }

        public ICommand CancelResultsCommand { get; }

        public ICommand ValidateResultsCommand { get; }

        public ICommand EditResultsCommand { get; }

        public ICommand SelectConflictsCommand { get; }

        public ICommand SelectFixtureMatchesCommand { get; }

        private async Task OpenSchedulingAssistantAsync()
        {
            var filters = (MatchesPlanningFiltersViewModel)Filters;
            var displayDate = Display.Mode switch
            {
                DisplayModeDay displayModeDay => (DateOnly?)displayModeDay.DisplayDate.ToDate(),
                DisplayModeByDate => filters.DateFilter.Item.CastIn<DateFilterViewModel>().Value,
                DisplayModeByStage => filters.CompetitionStageFilter.Item.CastIn<CompetitionStageFilterViewModel>().Value?.StartDate.ToDate(),
                _ => null,
            };
            await OpenSchedulingAssistantAsync(displayDate).ConfigureAwait(false);
        }

        private async Task OpenSchedulingAssistantAsync(DateOnly? displayDate)
            => await _matchPresentationService.OpenSchedulingAssistantAsync(Source, displayDate).ConfigureAwait(false);

        private async Task RescheduleConflictsAsync(MatchViewModel match)
            => await OpenSchedulingAssistantAsync(match.DateOfDay).ConfigureAwait(false);

        public void Load(string displayMode, object? filterValue = null)
        {
            Display.SetMode(displayMode);

            var filters = (MatchesPlanningFiltersViewModel)Filters;
            if (filterValue is Guid id)
            {
                filters.Clear();
                var stage = filters.CompetitionStageFilter.Item.CastIn<CompetitionStageFilterViewModel>().AvailableValues?.GetByIdOrDefault(id);
                if (stage is not null)
                    filters.CompetitionStageFilter.Item.CastIn<CompetitionStageFilterViewModel>().Value = stage;
            }
            else if (filterValue is DateOnly date)
            {
                filters.Clear();
                if (displayMode == nameof(DisplayModeByDate))
                {
                    var found = filters.DateFilter.Item.CastIn<DateFilterViewModel>().AvailableValues?.Any(x => x == date);
                    if (found.IsTrue())
                        filters.DateFilter.Item.CastIn<DateFilterViewModel>().Value = date;
                }
                else
                {
                    Display.AllowedModes.OfType<DisplayModeDay>().First().DisplayDate = date.BeginningOfDay();
                }
            }
            else if (filterValue is IEnumerable<IFilterViewModel> newFilters)
            {
                filters.Set(newFilters);
            }
        }

        private void ValidateStadiumsAvaibility(IEnumerable<MatchViewModel> matches)
        {
            foreach (var item in Stadiums.Items.OfType<StadiumWrapper>())
                item.Availability = CheckStadiumAvaibility(item.Stadium.Id, matches);
        }
        private AvailabilityCheck CheckStadiumAvaibility(Guid stadiumId, IEnumerable<MatchViewModel> matches)
            => matches.MaxOrDefault(x => _availibilityCheckingService.GetStadiumAvaibility(stadiumId, new(x.Date.ToUniversalTime(), x.EndDate.ToUniversalTime()), matches.Select(x => x.Id).ToList()), AvailabilityCheck.Unknown);

        private void SelectConflicts(MatchViewModel match) => SelectItems(match.MatchesInConflicts.Select(x => x.Id).ToList());

        private void SelectFixtureMatches(MatchViewModel match) => match.Fixture.IfNotNull(x => SelectItems(x.Matches.Select(x => x.Id).ToList()));

        private void StartEditResults()
        {
            UnselectAll();
            WrappersSource.OfType<EditableMatch>().ForEach(x => x.Reset());
            Mode = ScreenMode.Edition;
        }

        private void EndEditResults()
        {
            UnselectAll();
            Mode = ScreenMode.Read;
        }

        private async Task ValidateResultsAsync()
        {
            if (ValidateProperties())
            {
                var matches = WrappersSource.OfType<EditableMatch>()
                                      .Where(x => x.IsModified())
                                      .Select(x => new MatchDto
                                      {
                                          Id = x.Item.Id,
                                          HomeScore = x.HomeScore.Value,
                                          AfterExtraTime = x.AfterExtraTime,
                                          AwayScore = x.AwayScore.Value,
                                          AwayShootoutScore = x.AwayShootoutScore.Value,
                                          HomeShootoutScore = x.HomeShootoutScore.Value
                                      }).ToList();
                await _matchPresentationService.SaveScoresAsync(matches).ConfigureAwait(false);

                EndEditResults();
            }
            else
            {
                GetErrors().ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
            }
        }

        private void CancelResults()
        {
            WrappersSource.OfType<EditableMatch>().ForEach(x => x.Reset());
            EndEditResults();
        }

        protected override void Cleanup()
        {
            _matchesProvider.LoadRunner.Unregister(this);
            base.Cleanup();
        }
    }

    internal class MatchesCollection : SelectableCollection<MatchViewModel>
    {
        public MatchesCollection(ISourceProvider<MatchViewModel> matchesSourceProvider) : base(matchesSourceProvider, scheduler: Scheduler.UI, createWrapper: x => new EditableMatch(x)) { }
    }

    [CanSetIsModified(false)]
    [CanBeValidated(false)]
    internal class EditableMatch : SelectedWrapper<MatchViewModel>, IAppointment
    {
        public EditableMatch(MatchViewModel item) : base(item)
        {
            Reset();

            UpCommand = CommandsManager.CreateNotNull<AcceptableValue<int>>(x => x.Value.IfNotNull(_ => x.Value += 1, () => x.Value = 1), x => Item.CanBe(MatchState.Played) && (!x.HasValue || x.Value < x.Max));
            DownCommand = CommandsManager.CreateNotNull<AcceptableValue<int>>(x => x.Value.IfNotNull(_ => x.Value -= 1, () => x.Value = 0), x => Item.CanBe(MatchState.Played) && (!x.HasValue || x > x.Min));

            Disposables.AddRange(
            [
                HomeScore.WhenPropertyChanged(x => x.Value, false).Subscribe(_ => OnScoreChanged()),
                AwayScore.WhenPropertyChanged(x => x.Value, false).Subscribe(_ => OnScoreChanged()),
            ]);
        }

        public DateTime StartDate => Item.Date;

        public DateTime EndDate => Item.EndDate;

        [Display(Name = nameof(HomeScore), ResourceType = typeof(MyClubResources))]
        [CanSetIsModified(true)]
        [CanBeValidated(true)]
        public AcceptableValue<int> HomeScore { get; } = new AcceptableValue<int>(Match.AcceptableRangeScore);

        [Display(Name = nameof(AwayScore), ResourceType = typeof(MyClubResources))]
        [CanSetIsModified(true)]
        [CanBeValidated(true)]
        public AcceptableValue<int> AwayScore { get; } = new AcceptableValue<int>(Match.AcceptableRangeScore);

        [Display(Name = nameof(HomeScore), ResourceType = typeof(MyClubResources))]
        [CanSetIsModified(true)]
        [CanBeValidated(true)]
        public AcceptableValue<int> HomeShootoutScore { get; } = new AcceptableValue<int>(Match.AcceptableRangeScore);

        [Display(Name = nameof(AwayScore), ResourceType = typeof(MyClubResources))]
        [CanSetIsModified(true)]
        [CanBeValidated(true)]
        public AcceptableValue<int> AwayShootoutScore { get; } = new AcceptableValue<int>(Match.AcceptableRangeScore);

        [Display(Name = nameof(AfterExtraTime), ResourceType = typeof(MyClubResources))]
        [CanSetIsModified(true)]
        [CanBeValidated(true)]
        public bool AfterExtraTime { get; set; }

        public bool HasDraw => HomeScore == AwayScore;

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditExtraTimeOrShootout => (Item.Format.ExtraTimeIsEnabled || Item.Format.ShootoutIsEnabled) && HasDraw;

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEdit => Item.CanBe(MatchState.Played);

        public ICommand UpCommand { get; }

        public ICommand DownCommand { get; }

        [SuppressPropertyChangedWarnings]
        private void OnScoreChanged()
        {
            if (IsModifiedSuspender.IsSuspended) return;

            RaisePropertyChanged(nameof(HasDraw));
            RaisePropertyChanged(nameof(CanEditExtraTimeOrShootout));
            RaisePropertyChanged(nameof(CanEdit));
            if (HasDraw)
                AfterExtraTime = false;
        }

        public void Reset()
        {
            if (Item.HasResult)
            {
                HomeScore.Value = Item.Home.Score;
                AwayScore.Value = Item.Away.Score;
                HomeShootoutScore.Value = Item.Home.ShootoutScore;
                AwayShootoutScore.Value = Item.Away.ShootoutScore;
                AfterExtraTime = Item.AfterExtraTime;
            }
            else
            {
                HomeScore.Reset();
                AwayScore.Reset();
                HomeShootoutScore.Reset();
                AwayShootoutScore.Reset();
                AfterExtraTime = false;
            }
        }
    }
}

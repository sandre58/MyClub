// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Wpf.Filters;
using MyClub.Scorer.Wpf.Services;
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
using MyNet.Utilities;
using MyNet.Utilities.Localization;
using MyNet.Utilities.Units;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal class MatchesPlanningViewModel : SelectionListViewModel<MatchViewModel>
    {
        private readonly MatchPresentationService _matchPresentationService;
        private readonly AvailibilityCheckingService _availibilityCheckingService;

        public MatchesPlanningViewModel(SchedulingParametersViewModel schedulingParameters,
                                        ISourceProvider<MatchViewModel> matchesProvider,
                                        ISourceProvider<IMatchParent> parentsProvider,
                                        ISourceProvider<ITeamViewModel> teamsProvider,
                                        ISourceProvider<IStadiumViewModel> stadiumsProvider,
                                        MatchPresentationService matchPresentationService,
                                        CompetitionCommandsService competitionCommandsService,
                                        AvailibilityCheckingService availibilityCheckingService)
            : base(collection: new MatchesCollection(matchesProvider),
                  parametersProvider: new MatchesPlanningListParametersProvider(parentsProvider.Source,
                                                                                new ObservableSourceProvider<DateOnly>(matchesProvider.Connect()
                                                                                                                                      .AutoRefreshOnObservable(x => Observable.FromEventPattern(x => GlobalizationService.Current.TimeZoneChanged += x, x => GlobalizationService.Current.TimeZoneChanged -= x))
                                                                                                                                      .AutoRefreshOnObservable(x => x.WhenPropertyChanged(y => y.DateOfDay))
                                                                                                                                      .DistinctValues(x => x.DateOfDay)
                                                                                                                                      .ObserveOn(Scheduler.UI)).Source,
                                                                                teamsProvider.Source,
                                                                                stadiumsProvider.Source,
                                                                                schedulingParameters))
        {
            _matchPresentationService = matchPresentationService;
            _availibilityCheckingService = availibilityCheckingService;
            Stadiums = new ListViewModel<IStadiumWrapper>(stadiumsProvider.Connect().Transform(x => (IStadiumWrapper)new StadiumWrapper(x)).Merge(new ObservableCollection<IStadiumWrapper>() { new AutomaticStadiumWrapper(), new NoStadiumWrapper() }.ToObservableChangeSet()));
            Mode = ScreenMode.Read;
            CanPage = true;
            CanAdd = false;
            CanRemove = false;

            SelectAllByParentCommand = CommandsManager.CreateNotNull<IMatchParent>(x => Collection.Select(Items.Where(y => y.Parent == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.Parent == x && y.IsSelectable).Any(y => !y.IsSelected));
            UnselectAllByParentCommand = CommandsManager.CreateNotNull<IMatchParent>(x => Collection.Unselect(Items.Where(y => y.Parent == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.Parent == x && y.IsSelectable).All(y => y.IsSelected));
            SelectAllByDateCommand = CommandsManager.CreateNotNull<DateOnly>(x => Collection.Select(Items.Where(y => y.DateOfDay == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.DateOfDay == x && y.IsSelectable).Any(y => !y.IsSelected));
            UnselectAllByDateCommand = CommandsManager.CreateNotNull<DateOnly>(x => Collection.Unselect(Items.Where(y => y.DateOfDay == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.DateOfDay == x && y.IsSelectable).All(y => y.IsSelected));
            EditResultsCommand = CommandsManager.Create(StartEditResults, () => Mode == ScreenMode.Read && Items.Count > 0);
            ValidateResultsCommand = CommandsManager.Create(async () => await ValidateResultsAsync().ConfigureAwait(false), () => Mode == ScreenMode.Edition);
            CancelResultsCommand = CommandsManager.Create(CancelResults, () => Mode == ScreenMode.Edition);
            StartSelectedItemsCommand = CommandsManager.Create(async () => await StartSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanBe(MatchState.InProgress)));
            SuspendSelectedItemsCommand = CommandsManager.Create(async () => await SuspendSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanBe(MatchState.Suspended)));
            PostponeSelectedItemsCommand = CommandsManager.Create(async () => await PostponeSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanBe(MatchState.Postponed)));
            CancelSelectedItemsCommand = CommandsManager.Create(async () => await CancelSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanBe(MatchState.Cancelled)));
            FinishSelectedItemsCommand = CommandsManager.Create(async () => await FinishSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanBe(MatchState.Played)));
            ResetSelectedItemsCommand = CommandsManager.Create(async () => await ResetSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanBe(MatchState.None)));
            DoWithdrawForHomeTeamSelectedItemsCommand = CommandsManager.Create(async () => await DoWithdrawForHomeTeamSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanDoWithdraw()));
            DoWithdrawForAwayTeamSelectedItemsCommand = CommandsManager.Create(async () => await DoWithdrawForAwayTeamSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanDoWithdraw()));
            RandomizeSelectedItemsCommand = CommandsManager.Create(async () => await RandomizeSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanRandomize()));
            InvertTeamsSelectedItemsCommand = CommandsManager.Create(async () => await InvertTeamsSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanInvertTeams()));
            RescheduleCommand = CommandsManager.CreateNotNull<object[]>(async x => await RescheduleSelectedItemsAsync(Convert.ToInt32(x[0]), (TimeUnit)x[1]).ConfigureAwait(false), x => x.Length == 2 && x[0] is double && x[1] is TimeUnit && SelectionIsAvailable(x => x.CanReschedule()));
            RescheduleXMinutesCommand = CommandsManager.CreateNotNull<int>(async x => await RescheduleSelectedItemsAsync(x, TimeUnit.Minute).ConfigureAwait(false), x => SelectionIsAvailable(x => x.CanReschedule()));
            RescheduleXHoursCommand = CommandsManager.CreateNotNull<int>(async x => await RescheduleSelectedItemsAsync(x, TimeUnit.Hour).ConfigureAwait(false), x => SelectionIsAvailable(x => x.CanReschedule()));
            RescheduleAutomaticCommand = CommandsManager.Create(async () => await RescheduleAutomaticSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanRescheduleAutomatic()));
            SelectConflictsCommand = CommandsManager.CreateNotNull<MatchViewModel>(x => SelectConflicts(x), x => x.MatchesInConflicts.Count > 0);
            RescheduleConflictsCommand = CommandsManager.CreateNotNull<MatchViewModel>(async x => await RescheduleConflictsAsync(x).ConfigureAwait(false), x => x.MatchesInConflicts.Count > 0);
            SetStadiumForSelectedItemsCommand = CommandsManager.CreateNotNull<IStadiumWrapper>(async x => await SetStadiumForSelectedItemsAsync(x).ConfigureAwait(false), x => SelectionIsAvailable(y => y.CanReschedule() && (x is not AutomaticStadiumWrapper || y.CanRescheduleAutomaticStadium())));
            OpenSchedulingAssistantCommand = CommandsManager.Create(async () => await OpenSchedulingAssistantAsync().ConfigureAwait(false));
            EditSchedulingParametersCommand = CommandsManager.Create(async () => await competitionCommandsService.EditSchedulingParametersAsync().ConfigureAwait(false), () => Mode == ScreenMode.Read);

            Disposables.AddRange(
            [
                Items.ToObservableChangeSet().WhenPropertyChanged(x => x.State).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(CanDoWithdrawSelectedItems));
                    RaisePropertyChanged(nameof(CanRescheduleSelectedItems));
                }),
                SelectedWrappers.ToObservableChangeSet().Throttle(1000.Milliseconds()).Subscribe(_ => ValidateStadiumsAvaibility(SelectedItems.ToList()))
            ]);
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanDoWithdrawSelectedItems => SelectionIsAvailable(x => x.CanDoWithdraw());

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanRescheduleSelectedItems => SelectionIsAvailable(x => x.CanReschedule());

        [CanBeValidated]
        [CanSetIsModified]
        public ListViewModel<IStadiumWrapper> Stadiums { get; }

        public ICommand SelectAllByParentCommand { get; }

        public ICommand UnselectAllByParentCommand { get; }

        public ICommand SelectAllByDateCommand { get; }

        public ICommand UnselectAllByDateCommand { get; }

        public ICommand CancelResultsCommand { get; }

        public ICommand ValidateResultsCommand { get; }

        public ICommand EditResultsCommand { get; }

        public ICommand EditSchedulingParametersCommand { get; }

        public ICommand ResetSelectedItemsCommand { get; }

        public ICommand StartSelectedItemsCommand { get; }

        public ICommand PostponeSelectedItemsCommand { get; }

        public ICommand CancelSelectedItemsCommand { get; }

        public ICommand SuspendSelectedItemsCommand { get; }

        public ICommand FinishSelectedItemsCommand { get; }

        public ICommand DoWithdrawForHomeTeamSelectedItemsCommand { get; }

        public ICommand DoWithdrawForAwayTeamSelectedItemsCommand { get; }

        public ICommand RandomizeSelectedItemsCommand { get; }

        public ICommand InvertTeamsSelectedItemsCommand { get; }

        public ICommand RescheduleXMinutesCommand { get; }

        public ICommand RescheduleXHoursCommand { get; }

        public ICommand RescheduleCommand { get; }

        public ICommand RescheduleAutomaticCommand { get; }

        public ICommand OpenSchedulingAssistantCommand { get; }

        public ICommand SelectConflictsCommand { get; }

        public ICommand RescheduleConflictsCommand { get; }

        public ICommand SetStadiumForSelectedItemsCommand { get; }

        protected override async Task<MatchViewModel?> UpdateItemAsync(MatchViewModel oldItem)
        {
            await _matchPresentationService.EditAsync(oldItem).ConfigureAwait(false);

            return null;
        }

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

        public async Task StartSelectedItemsAsync() => await _matchPresentationService.StartAsync(SelectedItems).ConfigureAwait(false);

        public async Task ResetSelectedItemsAsync() => await _matchPresentationService.ResetAsync(SelectedItems).ConfigureAwait(false);

        public async Task CancelSelectedItemsAsync() => await _matchPresentationService.CancelAsync(SelectedItems).ConfigureAwait(false);

        public async Task PostponeSelectedItemsAsync() => await _matchPresentationService.PostponeAsync(SelectedItems).ConfigureAwait(false);

        public async Task SuspendSelectedItemsAsync() => await _matchPresentationService.SuspendAsync(SelectedItems).ConfigureAwait(false);

        public async Task FinishSelectedItemsAsync() => await _matchPresentationService.FinishAsync(SelectedItems).ConfigureAwait(false);

        public async Task RescheduleSelectedItemsAsync(int offset, TimeUnit timeUnit) => await _matchPresentationService.RescheduleAsync(SelectedItems, offset, timeUnit).ConfigureAwait(false);

        public async Task RescheduleAutomaticSelectedItemsAsync() => await _matchPresentationService.RescheduleAutomaticAsync(SelectedItems).ConfigureAwait(false);

        public async Task DoWithdrawForHomeTeamSelectedItemsAsync() => await _matchPresentationService.DoWithdrawForHomeTeamAsync(SelectedItems).ConfigureAwait(false);

        public async Task DoWithdrawForAwayTeamSelectedItemsAsync() => await _matchPresentationService.DoWithdrawForAwayTeamAsync(SelectedItems).ConfigureAwait(false);

        public async Task RandomizeSelectedItemsAsync() => await _matchPresentationService.RandomizeAsync(SelectedItems).ConfigureAwait(false);

        public async Task InvertTeamsSelectedItemsAsync() => await _matchPresentationService.InvertTeamsAsync(SelectedItems).ConfigureAwait(false);

        public async Task SetStadiumForSelectedItemsAsync(IStadiumWrapper stadiumWrapper)
        {
            if (stadiumWrapper is AutomaticStadiumWrapper)
                await _matchPresentationService.RescheduleAutomaticStadiumAsync(SelectedItems).ConfigureAwait(false);
            else
                await _matchPresentationService.SetStadiumAsync(SelectedItems, (stadiumWrapper as StadiumWrapper)?.Stadium).ConfigureAwait(false);
        }

        private async Task RescheduleConflictsAsync(MatchViewModel match)
            => await OpenSchedulingAssistantAsync(match.DateOfDay).ConfigureAwait(false);

        private async Task OpenSchedulingAssistantAsync()
        {
            var filters = (MatchesPlanningFiltersViewModel)Filters;
            var displayDate = Display.Mode switch
            {
                DisplayModeDay displayModeDay => (DateOnly?)displayModeDay.DisplayDate.ToDate(),
                DisplayModeByDate => filters.DateFilter.Item.CastIn<DateFilterViewModel>().Value,
                DisplayModeByParent => filters.ParentFilter.Item.CastIn<MatchParentFilterViewModel>().Value?.Date.ToDate(),
                _ => null,
            };
            await OpenSchedulingAssistantAsync(displayDate).ConfigureAwait(false);
        }

        private async Task OpenSchedulingAssistantAsync(DateOnly? displayDate)
            => await _matchPresentationService.OpenSchedulingAssistantAsync(Source.Where(x => x.CanReschedule()), displayDate).ConfigureAwait(false);

        public void SelectItems(IEnumerable<Guid> selectedItems)
        {
            Mode = ScreenMode.Read;
            UpdateSelection(Source.Where(x => selectedItems.Contains(x.Id)));
        }

        private void SelectConflicts(MatchViewModel match) => SelectItems(match.MatchesInConflicts.Select(x => x.Id).ToList());

        private void ValidateStadiumsAvaibility(IEnumerable<MatchViewModel> matches)
        {
            foreach (var item in Stadiums.Items.OfType<StadiumWrapper>())
                item.Availability = CheckStadiumAvaibility(item.Stadium.Id, matches);
        }

        private AvailabilityCheck CheckStadiumAvaibility(Guid stadiumId, IEnumerable<MatchViewModel> matches)
            => matches.MaxOrDefault(x => _availibilityCheckingService.GetStadiumAvaibility(stadiumId, x.GetPeriod(), matches.Select(x => x.Id).ToList()), AvailabilityCheck.Unknown);

        protected override bool SelectionIsAvailable(Func<MatchViewModel, bool> predicate) => Mode == ScreenMode.Read && base.SelectionIsAvailable(predicate);

        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            RaisePropertyChanged(nameof(CanDoWithdrawSelectedItems));
            RaisePropertyChanged(nameof(CanRescheduleSelectedItems));
        }
    }

    internal class MatchesCollection : SelectableCollection<MatchViewModel>
    {
        public MatchesCollection(ISourceProvider<MatchViewModel> matchesSourceProvider) : base(matchesSourceProvider.Connect(), scheduler: Scheduler.UI, createWrapper: x => new EditableMatch(x)) { }
    }

    [CanSetIsModified(false)]
    [CanBeValidated(false)]
    internal class EditableMatch : SelectedWrapper<MatchViewModel>, IAppointment
    {
        public EditableMatch(MatchViewModel item) : base(item)
        {
            Reset();

            UpCommand = CommandsManager.CreateNotNull<AcceptableValue<int>>(x => x.Value.IfNotNull(_ => x.Value += 1, () => x.Value = 1), x => !x.HasValue || x.Value < x.Max);
            DownCommand = CommandsManager.CreateNotNull<AcceptableValue<int>>(x => x.Value.IfNotNull(_ => x.Value -= 1, () => x.Value = 0), x => !x.HasValue || x > x.Min);

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

        public ICommand UpCommand { get; }

        public ICommand DownCommand { get; }

        [SuppressPropertyChangedWarnings]
        private void OnScoreChanged()
        {
            if (IsModifiedSuspender.IsSuspended) return;

            RaisePropertyChanged(nameof(HasDraw));
            RaisePropertyChanged(nameof(CanEditExtraTimeOrShootout));
            if (HasDraw)
                AfterExtraTime = false;
        }

        public void Reset()
        {
            if (Item.HasResult)
            {
                HomeScore.Value = Item.HomeScore;
                AwayScore.Value = Item.AwayScore;
                HomeShootoutScore.Value = Item.HomeShootoutScore;
                AwayShootoutScore.Value = Item.AwayShootoutScore;
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

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.Observable.Threading;
using MyNet.Observable.Translatables;
using MyNet.UI.Commands;
using MyNet.UI.Selection;
using MyNet.UI.Selection.Models;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.Units;
using MyNet.Wpf.DragAndDrop;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal class MatchesPlanningViewModel : SelectionListViewModel<MatchViewModel>
    {
        private readonly MatchPresentationService _matchPresentationService;

        public MatchesPlanningViewModel(ISourceProvider<MatchViewModel> matchesProvider,
                                        ISourceProvider<IMatchParent> parentsProvider,
                                        ISourceProvider<TeamViewModel> teamsProvider,
                                        ISourceProvider<StadiumViewModel> stadiumsProvider,
                                        MatchPresentationService matchPresentationService)
            : base(collection: new MatchesCollection(matchesProvider),
                  parametersProvider: new MatchesPlanningListParametersProvider(parentsProvider.Source, new ObservableSourceProvider<DateTime>(matchesProvider.Connect().AutoRefresh(x => x.Date).DistinctValues(x => x.DateOfDay).ObserveOn(Scheduler.UI)).Source, teamsProvider.Source, stadiumsProvider.Source))
        {
            _matchPresentationService = matchPresentationService;
            Mode = ScreenMode.Read;
            CanPage = true;
            CanAdd = false;
            CanRemove = false;
            DropHandler = new(async (x, y) => await _matchPresentationService.RescheduleAsync(x.OfType<EditableMatch>().Select(z => z.Item), y).ConfigureAwait(false),
                              x => x.All(y => y is EditableMatch editableMatch && editableMatch.Item.CanBeRescheduled));

            SelectAllByParentCommand = CommandsManager.CreateNotNull<IMatchParent>(x => Collection.Select(Items.Where(y => y.Parent == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.Parent == x && y.IsSelectable).Any(y => !y.IsSelected));
            UnselectAllByParentCommand = CommandsManager.CreateNotNull<IMatchParent>(x => Collection.Unselect(Items.Where(y => y.Parent == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.Parent == x && y.IsSelectable).All(y => y.IsSelected));
            SelectAllByDateCommand = CommandsManager.CreateNotNull<DateTime>(x => Collection.Select(Items.Where(y => y.DateOfDay == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.DateOfDay == x && y.IsSelectable).Any(y => !y.IsSelected));
            UnselectAllByDateCommand = CommandsManager.CreateNotNull<DateTime>(x => Collection.Unselect(Items.Where(y => y.DateOfDay == x).ToList()), x => Mode == ScreenMode.Read && Wrappers.Where(y => y.Item.DateOfDay == x && y.IsSelectable).All(y => y.IsSelected));
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
            SelectConflictsCommand = CommandsManager.CreateNotNull<MatchViewModel>(x => SelectConflicts(x), x => x.MatchesInConflicts.Count > 0);
            EditConflictsCommand = CommandsManager.CreateNotNull<MatchViewModel>(async x => await _matchPresentationService.RescheduleAsync(x.MatchesInConflicts).ConfigureAwait(false), x => x.MatchesInConflicts.Count > 0);

            Disposables.AddRange(
            [
                Items.ToObservableChangeSet().WhenPropertyChanged(x => x.State).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(CanDoWithdrawSelectedItems));
                    RaisePropertyChanged(nameof(CanRescheduleSelectedItems));
                }),
            ]);
        }

        public CalendarDropHandler DropHandler { get; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanDoWithdrawSelectedItems => SelectionIsAvailable(x => x.CanDoWithdraw());

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanRescheduleSelectedItems => SelectionIsAvailable(x => x.CanReschedule());

        public ICommand SelectAllByParentCommand { get; }

        public ICommand UnselectAllByParentCommand { get; }

        public ICommand SelectAllByDateCommand { get; }

        public ICommand UnselectAllByDateCommand { get; }

        public ICommand CancelResultsCommand { get; }

        public ICommand ValidateResultsCommand { get; }

        public ICommand EditResultsCommand { get; }

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

        public ICommand SelectConflictsCommand { get; }

        public ICommand EditConflictsCommand { get; }

        protected override async Task<MatchViewModel?> UpdateItemAsync(MatchViewModel oldItem)
        {
            await _matchPresentationService.EditAsync(oldItem).ConfigureAwait(false);

            return null;
        }

        protected override async Task<IEnumerable<MatchViewModel>> UpdateRangeAsync(IEnumerable<MatchViewModel> oldItems)
        {
            if (oldItems.Count() == 1)
                await _matchPresentationService.EditAsync(oldItems.First()).ConfigureAwait(false);
            else if (oldItems.Count() > 1)
                await _matchPresentationService.EditMultipleAsync(oldItems).ConfigureAwait(false);

            return [];
        }

        protected override void ResetCore() => Filters.Reset();

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

        public async Task DoWithdrawForHomeTeamSelectedItemsAsync() => await _matchPresentationService.DoWithdrawForHomeTeamAsync(SelectedItems).ConfigureAwait(false);

        public async Task DoWithdrawForAwayTeamSelectedItemsAsync() => await _matchPresentationService.DoWithdrawForAwayTeamAsync(SelectedItems).ConfigureAwait(false);

        public async Task RandomizeSelectedItemsAsync() => await _matchPresentationService.RandomizeAsync(SelectedItems).ConfigureAwait(false);

        public async Task InvertTeamsSelectedItemsAsync() => await _matchPresentationService.InvertTeamsAsync(SelectedItems).ConfigureAwait(false);

        public void SelectItems(IEnumerable<Guid> selectedItems)
        {
            Mode = ScreenMode.Read;
            UpdateSelection(Source.Where(x => selectedItems.Contains(x.Id)));
        }

        private void SelectConflicts(MatchViewModel match) => SelectItems(match.MatchesInConflicts.Select(x => x.Id).ToList());

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

        public DateTime StartDate => Item.StartDate;

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
            if (Item.State is MatchState.InProgress or MatchState.Suspended or MatchState.Played)
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

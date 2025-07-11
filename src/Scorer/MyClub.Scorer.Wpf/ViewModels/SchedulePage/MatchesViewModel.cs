// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Domain.Enums;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.UI.Selection;
using MyNet.UI.Threading;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal class MatchesViewModel : SelectionListViewModel<MatchViewModel>
    {
        private readonly MatchPresentationService _matchPresentationService;

        public MatchesViewModel(IObservable<IChangeSet<MatchViewModel>> observable,
                                MatchPresentationService matchPresentationService,
                                IListParametersProvider? parametersProvider = null)
            : this(new SelectableCollection<MatchViewModel>(observable, scheduler: Scheduler.GetUIOrCurrent()), matchPresentationService, parametersProvider: parametersProvider) { }

        public MatchesViewModel(ISourceProvider<MatchViewModel> source,
                                MatchPresentationService matchPresentationService,
                                IListParametersProvider? parametersProvider = null)
            : this(new SelectableCollection<MatchViewModel>(source, scheduler: Scheduler.GetUIOrCurrent()), matchPresentationService, parametersProvider: parametersProvider) { }

        public MatchesViewModel(SelectableCollection<MatchViewModel> collection,
                                MatchPresentationService matchPresentationService,
                                IListParametersProvider? parametersProvider = null)
            : base(collection, parametersProvider: parametersProvider)
        {
            _matchPresentationService = matchPresentationService;
            Mode = ScreenMode.Read;
            CanPage = true;
            CanAdd = false;
            CanRemove = false;

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
            RescheduleOnDateCommand = CommandsManager.CreateNotNull<object[]>(async x => await RescheduleSelectedItemsAsync(((DateTime?)x[0])?.ToDate(), ((DateTime?)x[1])?.ToTime()).ConfigureAwait(false), x => x.Length == 2 && (x[0] is DateTime? || x[1] is DateTime?) && SelectionIsAvailable(x => x.CanReschedule()));
            RescheduleXMinutesCommand = CommandsManager.CreateNotNull<int>(async x => await RescheduleSelectedItemsAsync(x, TimeUnit.Minute).ConfigureAwait(false), x => SelectionIsAvailable(x => x.CanReschedule()));
            RescheduleXHoursCommand = CommandsManager.CreateNotNull<int>(async x => await RescheduleSelectedItemsAsync(x, TimeUnit.Hour).ConfigureAwait(false), x => SelectionIsAvailable(x => x.CanReschedule()));
            RescheduleAutomaticCommand = CommandsManager.Create(async () => await RescheduleAutomaticSelectedItemsAsync().ConfigureAwait(false), () => SelectionIsAvailable(x => x.CanRescheduleAutomatic()));
            SetStadiumForSelectedItemsCommand = CommandsManager.CreateNotNull<IStadiumWrapper>(async x => await SetStadiumForSelectedItemsAsync(x).ConfigureAwait(false), x => SelectionIsAvailable(y => y.CanReschedule() && (x is not AutomaticStadiumWrapper || y.CanRescheduleAutomaticStadium())));

            Disposables.AddRange(
            [
                SelectedWrappers.ToObservableChangeSet().WhenPropertyChanged(x => x.Item.State).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(CanDoWithdrawSelectedItems));
                    RaisePropertyChanged(nameof(CanRescheduleSelectedItems));
                }),
            ]);
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanDoWithdrawSelectedItems => SelectionIsAvailable(x => x.CanDoWithdraw());

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanRescheduleSelectedItems => SelectionIsAvailable(x => x.CanReschedule());

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

        public ICommand RescheduleOnDateCommand { get; }

        public ICommand RescheduleAutomaticCommand { get; }

        public ICommand SetStadiumForSelectedItemsCommand { get; }

        protected override async Task<MatchViewModel?> UpdateItemAsync(MatchViewModel oldItem)
        {
            await _matchPresentationService.EditAsync(oldItem).ConfigureAwait(false);

            return null;
        }

        protected override async void OpenCore(MatchViewModel item, int? selectedTab = null) => await item.OpenAsync().ConfigureAwait(false);

        public async Task StartSelectedItemsAsync() => await _matchPresentationService.StartAsync(SelectedItems).ConfigureAwait(false);

        public async Task ResetSelectedItemsAsync() => await _matchPresentationService.ResetAsync(SelectedItems).ConfigureAwait(false);

        public async Task CancelSelectedItemsAsync() => await _matchPresentationService.CancelAsync(SelectedItems).ConfigureAwait(false);

        public async Task PostponeSelectedItemsAsync() => await _matchPresentationService.PostponeAsync(SelectedItems).ConfigureAwait(false);

        public async Task SuspendSelectedItemsAsync() => await _matchPresentationService.SuspendAsync(SelectedItems).ConfigureAwait(false);

        public async Task FinishSelectedItemsAsync() => await _matchPresentationService.FinishAsync(SelectedItems).ConfigureAwait(false);

        public async Task RescheduleSelectedItemsAsync(int offset, TimeUnit timeUnit) => await _matchPresentationService.RescheduleAsync(SelectedItems, offset, timeUnit).ConfigureAwait(false);

        public async Task RescheduleSelectedItemsAsync(DateOnly? date, TimeOnly? time) => await _matchPresentationService.RescheduleAsync(SelectedItems, date, time).ConfigureAwait(false);

        public async Task RescheduleAutomaticSelectedItemsAsync() => await _matchPresentationService.RescheduleAutomaticAsync(SelectedItems).ConfigureAwait(false);

        public async Task DoWithdrawForHomeTeamSelectedItemsAsync() => await _matchPresentationService.DoWithdrawForHomeTeamsAsync(SelectedItems).ConfigureAwait(false);

        public async Task DoWithdrawForAwayTeamSelectedItemsAsync() => await _matchPresentationService.DoWithdrawForAwayTeamsAsync(SelectedItems).ConfigureAwait(false);

        public async Task RandomizeSelectedItemsAsync() => await _matchPresentationService.RandomizeAsync(SelectedItems).ConfigureAwait(false);

        public async Task InvertTeamsSelectedItemsAsync() => await _matchPresentationService.InvertTeamsAsync(SelectedItems).ConfigureAwait(false);

        public async Task SetStadiumForSelectedItemsAsync(IStadiumWrapper stadiumWrapper)
        {
            if (stadiumWrapper is AutomaticStadiumWrapper)
                await _matchPresentationService.RescheduleAutomaticStadiumAsync(SelectedItems).ConfigureAwait(false);
            else
                await _matchPresentationService.SetStadiumAsync(SelectedItems, (stadiumWrapper as StadiumWrapper)?.Stadium).ConfigureAwait(false);
        }

        public void SelectItems(IEnumerable<Guid> selectedItems)
        {
            Mode = ScreenMode.Read;
            UpdateSelection(Source.Where(x => selectedItems.Contains(x.Id)));
        }

        protected override bool SelectionIsAvailable(Func<MatchViewModel, bool> predicate) => Mode == ScreenMode.Read && base.SelectionIsAvailable(predicate);

        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            RaisePropertyChanged(nameof(CanDoWithdrawSelectedItems));
            RaisePropertyChanged(nameof(CanRescheduleSelectedItems));
        }

        public IDisposable DeferRefresh() => Collection.DeferRefresh();
    }
}

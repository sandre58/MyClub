// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.UI.Selection;
using MyNet.UI.Selection.Models;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.Wpf.Schedulers;
using MyNet.Utilities;
using MyNet.Observable;
using MyNet.UI.Threading;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.SessionsTab
{
    internal class TrainingSessionsViewModel : SelectionListViewModel<TrainingSessionViewModel>
    {
        private readonly TrainingSessionPresentationService _trainingSessionPresentationService;
        private readonly ReadOnlyObservableCollection<HolidaysViewModel> _holidays;
        private readonly ReadOnlyObservableCollection<CycleViewModel> _cycles;

        public ReadOnlyObservableCollection<CycleViewModel> Cycles => _cycles;

        public ReadOnlyObservableCollection<HolidaysViewModel> Holidays => _holidays;

        public bool ShowHolidays { get; set; } = true;

        public bool ShowCycles { get; set; }

        public IEnumerable? SelectedDates { get; set; }

        public ICommand EditAttendancesSelectedItemCommand { get; set; }

        public ICommand AddToDateCommand { get; set; }

        public ICommand DuplicateSelectedItemCommand { get; set; }

        public ICommand RemoveSessionsInSelectedDatesCommand { get; set; }

        public ICommand CancelSelectedItemsCommand { get; private set; }

        public ICommand InitializeAttendancesSelectedItemsCommand { get; private set; }

        public ICommand CancelSessionsInSelectedDatesCommand { get; set; }

        public ICommand SelectSessionsInSelectedDatesCommand { get; set; }

        public ICommand UnselectSessionsInSelectedDatesCommand { get; set; }

        public ICommand MoveToPreviousDateCommand { get; }

        public ICommand MoveToNextDateCommand { get; }

        public ICommand MoveToTodayCommand { get; }

        public override bool CanAdd => base.CanAdd && (Display.Mode is not DisplayModeCalendar || SelectedDates is not null && SelectedDates.OfType<DateTime>().Any());

        public TrainingSessionsViewModel(
            TrainingSessionsProvider trainingSessionsProvider,
            HolidaysProvider holidaysProvider,
            CyclesProvider cyclesProvider,
            TrainingSessionPresentationService trainingSessionPresentationService)
            : base(collection: new TrainingSessionsCollection(trainingSessionsProvider),
                  parametersProvider: new TrainingSessionsListParametersProvider(holidaysProvider))
        {
            _trainingSessionPresentationService = trainingSessionPresentationService;

            DuplicateSelectedItemCommand = CommandsManager.Create(async () => await _trainingSessionPresentationService.DuplicateAsync(SelectedItem!).ConfigureAwait(false), () => SelectedItems.Count() == 1);
            EditAttendancesSelectedItemCommand = CommandsManager.Create(async () => await _trainingSessionPresentationService.EditAttendancesAsync(SelectedItem!).ConfigureAwait(false), () => SelectedItems.Count() == 1 && !SelectedItem!.IsCancelled);
            RemoveSessionsInSelectedDatesCommand = CommandsManager.Create(async () => await RemoveRangeAsync(GetItemsInSelectedDates()).ConfigureAwait(false), () => GetItemsInSelectedDates().Count != 0);
            InitializeAttendancesSelectedItemsCommand = CommandsManager.Create(async () => await InitializeAttendancesAsync(SelectedItems.Where(x => x.CanInitializeAttendances()).ToList()).ConfigureAwait(false), () => SelectedItems.Any(x => x.CanInitializeAttendances()));
            CancelSelectedItemsCommand = CommandsManager.Create(async () => await CancelAsync(SelectedItems).ConfigureAwait(false), () => SelectedItems.Any(x => x.CanCancel()));
            CancelSessionsInSelectedDatesCommand = CommandsManager.Create(async () => await CancelAsync(GetItemsInSelectedDates()).ConfigureAwait(false), () => GetItemsInSelectedDates().Exists(x => x.CanCancel()));
            SelectSessionsInSelectedDatesCommand = CommandsManager.Create(() => Collection.Select(GetItemsInSelectedDates()), () => GetItemsInSelectedDates().Count != 0);
            UnselectSessionsInSelectedDatesCommand = CommandsManager.Create(() => Collection.Unselect(GetItemsInSelectedDates()), () => GetItemsInSelectedDates().Count != 0);
            AddToDateCommand = CommandsManager.Create<DateTime>(async x => await AddToDateAsync(x).ConfigureAwait(false));
            MoveToPreviousDateCommand = CommandsManager.Create(MoveToPreviousDate);
            MoveToNextDateCommand = CommandsManager.Create(MoveToNextDate);
            MoveToTodayCommand = CommandsManager.Create(MoveToToday);

            Disposables.AddRange(
            [
                holidaysProvider.Items.ToObservableChangeSet(x => x.Id)
                                      .AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(nameof(HolidaysViewModel.StartDate), nameof(HolidaysViewModel.EndDate)))
                                      .ObserveOn(WpfScheduler.Current)
                                      .Bind(out _holidays)
                                      .Subscribe(_ =>
                                      {
                                         Filters.Refresh();
                                          RaisePropertyChanged(nameof(Holidays));
                                      }),
                cyclesProvider.Items.ToObservableChangeSet(x => x.Id)
                                            .AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(nameof(CycleViewModel.StartDate), nameof(CycleViewModel.EndDate)))
                                            .ObserveOn(WpfScheduler.Current)
                                            .Bind(out _cycles)
                                            .Subscribe(_ =>
                                            {
                                               Filters.Refresh();
                                                RaisePropertyChanged(nameof(Cycles));
                                            })
            ]);
        }

        protected override async Task<TrainingSessionViewModel?> CreateNewItemAsync()
        {
            var id = await _trainingSessionPresentationService.AddAsync().ConfigureAwait(false);

            return Source.GetByIdOrDefault(id.GetValueOrDefault());
        }

        protected override void OnAddCompleted(TrainingSessionViewModel item)
        {
            if (Items.Contains(item))
                Collection.SetSelection([item]);
        }

        protected override async Task<TrainingSessionViewModel?> UpdateItemAsync(TrainingSessionViewModel oldItem)
        {
            await _trainingSessionPresentationService.EditAsync(oldItem).ConfigureAwait(false);

            return null;
        }

        protected override async Task<IEnumerable<TrainingSessionViewModel>> UpdateRangeAsync(IEnumerable<TrainingSessionViewModel> oldItems)
        {
            if (oldItems.Count() == 1)
                await _trainingSessionPresentationService.EditAsync(oldItems.First()).ConfigureAwait(false);
            else if (oldItems.Count() > 1)
                await _trainingSessionPresentationService.EditMultipleAsync(oldItems).ConfigureAwait(false);

            return [];
        }

        public override async Task AddAsync()
        {
            if (Display.Mode is not DisplayModeCalendar)
                await base.AddAsync().ConfigureAwait(false);
            else if (SelectedDates is not null && SelectedDates.OfType<DateTime>().Count() == 1)
                await AddToDateAsync(SelectedDates.OfType<DateTime>().First()).ConfigureAwait(false);
            else if (SelectedDates is not null && SelectedDates.OfType<DateTime>().Any())
            {
                await _trainingSessionPresentationService.AddMultipleAsync(SelectedDates.OfType<DateTime>()).ConfigureAwait(false);
            }
        }

        private async Task AddToDateAsync(DateTime date)
        {
            var id = await _trainingSessionPresentationService.AddAsync(date).ConfigureAwait(false);

            if (Source.GetByIdOrDefault(id.GetValueOrDefault()) is TrainingSessionViewModel item)
                OnAddCompleted(item);
        }

        public override async Task RemoveRangeAsync(IEnumerable<TrainingSessionViewModel> oldItems) => await _trainingSessionPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        private async Task CancelAsync(IEnumerable<TrainingSessionViewModel> trainings) => await _trainingSessionPresentationService.CancelAsync(trainings).ConfigureAwait(false);

        private async Task InitializeAttendancesAsync(IEnumerable<TrainingSessionViewModel> trainings) => await _trainingSessionPresentationService.InitializeAttendancesAsync(trainings).ConfigureAwait(false);

        private void MoveToToday() => ((DisplayModeCalendar)Display.Mode!).DisplayDate = DateTime.Today;

        private void MoveToNextDate() => ((DisplayModeCalendar)Display.Mode!).DisplayDate = GetNextDate();

        private void MoveToPreviousDate() => ((DisplayModeCalendar)Display.Mode!).DisplayDate = GetPreviousDate();

        private DateTime GetNextDate() => ((DisplayModeCalendar)Display.Mode!).DisplayDate.AddMonths(1);

        private DateTime GetPreviousDate() => ((DisplayModeCalendar)Display.Mode!).DisplayDate.AddMonths(-1);

        private List<TrainingSessionViewModel> GetItemsInSelectedDates() => SelectedDates is null || !SelectedDates.OfType<DateTime>().Any()
                ? []
                : Items.Where(x => SelectedDates.OfType<DateTime>().Contains(x.StartDate.Date)).ToList();
    }

    internal class TrainingSessionsCollection : SelectableCollection<TrainingSessionViewModel>
    {
        public TrainingSessionsCollection(TrainingSessionsProvider trainingsProvider)
            : base(trainingsProvider.Connect(), scheduler: Scheduler.UI, createWrapper: x => new SelectedSessionAppointment(x)) { }
    }

    internal class SelectedSessionAppointment : SelectedWrapper<TrainingSessionViewModel>, IAppointment
    {
        public SelectedSessionAppointment(TrainingSessionViewModel item) : base(item) { }

        public DateTime StartDate => Item.StartDate;

        public DateTime EndDate => Item.EndDate;

        public DateTime Month => Item.StartDate.BeginningOfMonth();
    }
}

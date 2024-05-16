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
using MyNet.Observable.Collections.Providers;
using MyNet.Observable;
using MyNet.Observable.Threading;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal abstract class CompetitionPageMatchdaysViewModel : SelectionListViewModel<IMatchdayViewModel>
    {
        private readonly ReadOnlyObservableCollection<HolidaysViewModel> _holidays;
        private readonly MatchPresentationService _matchPresentationService;

        protected CompetitionPageMatchdaysViewModel(MatchPresentationService matchPresentationService,
                                                 HolidaysProvider holidaysProvider,
                                                 SelectableCollection<IMatchdayViewModel> collection)
            : base(collection: collection, parametersProvider: new CompetitionMatchdaysListParametersProvider())
        {
            _matchPresentationService = matchPresentationService;

            PostponeCommand = CommandsManager.Create(async () => await PostponeAsync().ConfigureAwait(false), () => SelectedItems.Any(x => !x.IsPostponed));
            AddMatchesCommand = CommandsManager.Create(async () => await AddMatchesAsync().ConfigureAwait(false), () => SelectedWrappers.Count == 1);
            EditResultsCommand = CommandsManager.Create(async () => await EditResultsAsync().ConfigureAwait(false), () => SelectedItems.Any(x => x.AllMatches.Any()));
            DuplicateCommand = CommandsManager.Create(async () => await DuplicateAsync().ConfigureAwait(false), () => CanDuplicate);
            AddMultipleCommand = CommandsManager.Create(async () => await AddMultipleAsync().ConfigureAwait(false));
            RemoveMatchdaysInSelectedDatesCommand = CommandsManager.Create(async () => await RemoveRangeAsync(GetItemsInSelectedDates()).ConfigureAwait(false), () => GetItemsInSelectedDates().Count != 0);
            SelectMatchdaysInSelectedDatesCommand = CommandsManager.Create(() => Collection.Select(GetItemsInSelectedDates()), () => GetItemsInSelectedDates().Count != 0);
            UnselectMatchdaysInSelectedDatesCommand = CommandsManager.Create(() => Collection.Unselect(GetItemsInSelectedDates()), () => GetItemsInSelectedDates().Count != 0);
            AddToDateCommand = CommandsManager.Create<DateTime>(async x => await AddToDateAsync(x).ConfigureAwait(false));
            MoveToPreviousDateCommand = CommandsManager.Create(MoveToPreviousDate, CanMoveToPreviousDate);
            MoveToNextDateCommand = CommandsManager.Create(MoveToNextDate, CanMoveToNextDate);
            MoveToTodayCommand = CommandsManager.Create(MoveToToday);

            Disposables.AddRange(
            [
                holidaysProvider.Items.ToObservableChangeSet(x => x.Id)
                                      .AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(nameof(HolidaysViewModel.StartDate), nameof(HolidaysViewModel.EndDate)))
                                      .ObserveOn(WpfScheduler.Current)
                                      .Bind(out _holidays)
                                      .Subscribe(_ => RaisePropertyChanged(nameof(Holidays)))
            ]);
        }

        public ReadOnlyObservableCollection<HolidaysViewModel> Holidays => _holidays;

        public virtual bool CanDuplicate => SelectedItems.Any();

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool ShowHolidays { get; set; } = true;

        public IEnumerable? SelectedDates { get; set; }

        public ICommand DuplicateCommand { get; }

        public ICommand MoveToPreviousDateCommand { get; }

        public ICommand MoveToNextDateCommand { get; }

        public ICommand MoveToTodayCommand { get; }

        public ICommand RemoveMatchdaysInSelectedDatesCommand { get; }

        public ICommand SelectMatchdaysInSelectedDatesCommand { get; }

        public ICommand UnselectMatchdaysInSelectedDatesCommand { get; }

        public ICommand AddToDateCommand { get; }

        public ICommand AddMultipleCommand { get; }

        public ICommand AddMatchesCommand { get; }

        public ICommand EditResultsCommand { get; }

        public ICommand PostponeCommand { get; }

        protected abstract override Task<IMatchdayViewModel?> CreateNewItemAsync();

        protected abstract override Task<IMatchdayViewModel?> UpdateItemAsync(IMatchdayViewModel oldItem);

        protected abstract Task AddToDateAsync(DateTime date);

        public abstract Task AddMultipleAsync();

        public async Task AddMatchesAsync()
        {
            if (SelectedItem is not null)
                await _matchPresentationService.AddMultipleAsync(SelectedItem).ConfigureAwait(false);
        }

        public abstract Task DuplicateAsync();

        public abstract Task PostponeAsync();

        public async Task EditResultsAsync()
        {
            if (SelectedItems.Any())
                await _matchPresentationService.EditAsync(SelectedItems.SelectMany(x => x.AllMatches)).ConfigureAwait(false);
        }

        public abstract override Task RemoveRangeAsync(IEnumerable<IMatchdayViewModel> oldItems);

        private List<IMatchdayViewModel> GetItemsInSelectedDates() => !GetSelectedDates().Any()
                ? []
                : Items.Where(x => GetSelectedDates().Contains(x.StartDate.Date)).ToList();

        private void MoveToToday() => ((DisplayModeCalendar)Display.Mode!).DisplayDate = DateTime.Today;

        private void MoveToNextDate() => ((DisplayModeCalendar)Display.Mode!).DisplayDate = GetNextDate();

        private void MoveToPreviousDate() => ((DisplayModeCalendar)Display.Mode!).DisplayDate = GetPreviousDate();

        private bool CanMoveToPreviousDate() => GetPreviousDate().IsBetween(StartDate, EndDate);

        private bool CanMoveToNextDate() => GetNextDate().IsBetween(StartDate, EndDate);

        private DateTime GetNextDate() => ((DisplayModeCalendar)Display.Mode!).DisplayDate.AddMonths(1);

        private DateTime GetPreviousDate() => ((DisplayModeCalendar)Display.Mode!).DisplayDate.AddMonths(-1);

        public IEnumerable<DateTime> GetSelectedDates() => SelectedDates is not null ? SelectedDates.OfType<DateTime>() : [];
    }

    internal class MatchdaysCollection(SourceProvider<IMatchdayViewModel> matchdaysSourceProvider) : SelectableCollection<IMatchdayViewModel>(matchdaysSourceProvider.Connect(), scheduler: Scheduler.UI, createWrapper: x => new SelectedMatchdayAppointment(x))
    {
    }

    internal class SelectedMatchdayAppointment(IMatchdayViewModel item) : SelectedWrapper<IMatchdayViewModel>(item), IAppointment
    {
        public DateTime StartDate => Item.StartDate;

        public DateTime EndDate => Item.EndDate;

        public DateTime Month => Item.StartDate.BeginningOfMonth();
    }
}

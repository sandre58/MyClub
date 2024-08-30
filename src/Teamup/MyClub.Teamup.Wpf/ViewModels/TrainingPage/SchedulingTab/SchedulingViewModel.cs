// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Aggregation;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Services;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Humanizer;
using MyNet.UI.Resources;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.SchedulingTab
{
    internal class SchedulingViewModel : ListViewModel<ISchedulingPeriodViewModel>
    {
        private enum PeriodType
        {
            Cycle,

            Holidays
        }

        private readonly HolidaysPresentationService _holidaysPresentationService;
        private readonly CyclePresentationService _cyclePresentationService;
        private PeriodType? _currentType;

        public int CountCycles { get; private set; }

        public int CountHolidays { get; private set; }

        public IEnumerable? SelectedDates { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public ICommand AddCycleCommand { get; set; }

        public ICommand AddHolidaysCommand { get; set; }

        public ICommand RemovePeriodsInSelectedDatesCommand { get; set; }

        public ICommand EditPeriodInSelectedDatesCommand { get; set; }

        public SchedulingViewModel(
            HolidaysProvider holidaysProvider,
            CyclesProvider cyclesProvider,
            HolidaysPresentationService holidaysPresentationService,
            CyclePresentationService cyclePresentationService)
                : base(parametersProvider: new SchedulingListParametersProvider())
        {
            _holidaysPresentationService = holidaysPresentationService;
            _cyclePresentationService = cyclePresentationService;

            AddCycleCommand = CommandsManager.Create(async () => await AddCycleAsync().ConfigureAwait(false));
            AddHolidaysCommand = CommandsManager.Create(async () => await AddHolidaysAsync().ConfigureAwait(false));
            RemovePeriodsInSelectedDatesCommand = CommandsManager.Create(async () => await RemoveRangeAsync(GetPeriodsInSelectedDates<ISchedulingPeriodViewModel>()).ConfigureAwait(false), HasAnyPeriodInSelectedDates<ISchedulingPeriodViewModel>);
            EditPeriodInSelectedDatesCommand = CommandsManager.Create(async () => await EditAsync(GetPeriodsInSelectedDates<ISchedulingPeriodViewModel>().FirstOrDefault()).ConfigureAwait(false), HasOnePeriodInSelectedDates<ISchedulingPeriodViewModel>);

            Disposables.AddRange(
            [
                holidaysProvider.Connect().Count().Subscribe(x => CountHolidays = x),
                cyclesProvider.Connect().Count().Subscribe(x => CountCycles = x),
                holidaysProvider.Connect().OnItemAdded(Collection.Add).OnItemRemoved(x => Collection.Remove(x)).Subscribe(),
                cyclesProvider.Connect().OnItemAdded(Collection.Add).OnItemRemoved(x => Collection.Remove(x)).Subscribe()
            ]);
        }

        protected override async Task<ISchedulingPeriodViewModel?> CreateNewItemAsync()
        {
            if (_currentType is null) return null;

            var dates = Display.Mode is DisplayModeCalendar ? GetSelectedDates() : null;
            if (_currentType == PeriodType.Holidays)
            {
                await _holidaysPresentationService.AddAsync(dates?.MinOrDefault(StartDate), dates?.MaxOrDefault(StartDate.AddDays(7))).ConfigureAwait(false);
            }
            else if (_currentType == PeriodType.Cycle)
            {
                await _cyclePresentationService.AddAsync(dates?.MinOrDefault(StartDate), dates?.MaxOrDefault(StartDate.AddMonths(1))).ConfigureAwait(false);
            }

            return null;
        }

        protected override async Task<ISchedulingPeriodViewModel?> UpdateItemAsync(ISchedulingPeriodViewModel oldItem)
        {
            if (oldItem is HolidaysViewModel holidays)
            {
                await _holidaysPresentationService.EditAsync(holidays).ConfigureAwait(false);
            }
            else if (oldItem is CycleViewModel cycle)
            {
                await _cyclePresentationService.EditAsync(cycle).ConfigureAwait(false);
            }

            return null;
        }

        public override async Task RemoveRangeAsync(IEnumerable<ISchedulingPeriodViewModel> oldItems)
        {
            if (!oldItems.Any()) return;

            var cancel = await DialogManager.ShowQuestionAsync(nameof(MessageResources.XItemsRemovingQuestion).TranslateAndFormatWithCount(oldItems.Count())!, UiResources.Removing).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {

                await AppBusyManager.WaitAsync(() =>
                {
                    var cycles = oldItems.OfType<CycleViewModel>().ToList();
                    var holidays = oldItems.OfType<HolidaysViewModel>().ToList();

                    if (cycles.Count != 0)
                        _cyclePresentationService.Remove(cycles);

                    if (holidays.Count != 0)
                        _holidaysPresentationService.Remove(holidays);
                });
            }
        }

        private bool HasOnePeriodInSelectedDates<T>() where T : ISchedulingPeriodViewModel
            => Items.OfType<T>().Count(x => GetSelectedDates().Exists(y => y.IsBetween(x.StartDate, x.EndDate))) == 1;

        private bool HasAnyPeriodInSelectedDates<T>() where T : ISchedulingPeriodViewModel
        {
            if (SelectedDates is null) return false;

            var items = Items.Where(x => GetSelectedDates().Exists(y => y.IsBetween(x.StartDate, x.EndDate))).ToList();

            return items.Exists(x => x is T);
        }

        private List<T> GetPeriodsInSelectedDates<T>() where T : ISchedulingPeriodViewModel
            => Items.OfType<T>().Where(x => GetSelectedDates().Exists(y => y.IsBetween(x.StartDate, x.EndDate))).ToList();

        private List<DateTime> GetSelectedDates() => SelectedDates?.OfType<DateTime>().ToList() ?? [];

        private async Task AddHolidaysAsync() => await AddPeriodInSelectedDatesAsync(PeriodType.Holidays).ConfigureAwait(false);

        private async Task AddCycleAsync() => await AddPeriodInSelectedDatesAsync(PeriodType.Cycle).ConfigureAwait(false);

        private async Task AddPeriodInSelectedDatesAsync(PeriodType type)
        {
            _currentType = type;
            await AddAsync().ConfigureAwait(false);
        }
    }
}

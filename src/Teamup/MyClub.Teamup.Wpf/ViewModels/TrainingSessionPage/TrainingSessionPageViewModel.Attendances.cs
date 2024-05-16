// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingSessionPage
{
    internal class TrainingSessionPageAttendancesViewModel : SelectionListViewModel<TrainingAttendanceViewModel>
    {
        private readonly TrainingSessionPresentationService _trainingSessionPresentationService;

        public int CountPresents { get; private set; }

        public int CountAbsents { get; private set; }

        public int CountApologized { get; private set; }

        public int CountInjured { get; private set; }

        public int CountInHolidays { get; private set; }

        public int CountInSelection { get; private set; }

        public int CountResting { get; private set; }

        public int CountUnknown { get; private set; }

        public ICommand SetSelectedAttendancesToUnknownCommand { get; }

        public ICommand SetSelectedAttendancesToPresentCommand { get; }

        public ICommand SetSelectedAttendancesToAbsentCommand { get; }

        public ICommand SetSelectedAttendancesToApologyCommand { get; }

        public ICommand SetSelectedAttendancesToInHolidaysCommand { get; }

        public ICommand SetSelectedAttendancesToInSelectionCommand { get; }

        public ICommand SetSelectedAttendancesToRestingCommand { get; }

        public ICommand SetSelectedAttendancesToInjuredCommand { get; }

        public ICommand SelectAttendancesCommand { get; }

        public ICommand InitializeAttendancesCommand { get; }

        public ICommand EditAttendancesCommand { get; }

        public TrainingSessionPageAttendancesViewModel(TrainingSessionPresentationService trainingSessionPresentationService, Subject<TrainingSessionViewModel?> trainingSessionChanged)
            : base(source: new AttendancesByTrainingSourceProvider(trainingSessionChanged).Connect(),
                  parametersProvider: new TrainingSessionAttendancesListParametersProvider())
        {
            _trainingSessionPresentationService = trainingSessionPresentationService;
            CanFilter = false;
            CanAdd = false;
            CanEdit = false;

            TrainingSessionViewModel? currentSession = null;
            var obs = Items.ToObservableChangeSet(x => x.Id).AutoRefresh(x => x.Attendance);

            EditAttendancesCommand = CommandsManager.Create(async () => await _trainingSessionPresentationService.EditAttendancesAsync(currentSession!).ConfigureAwait(false), () => currentSession is not null && !currentSession.IsCancelled);
            InitializeAttendancesCommand = CommandsManager.Create(async () => await _trainingSessionPresentationService.InitializeAttendancesAsync([currentSession!]).ConfigureAwait(false), () => currentSession is not null && !currentSession.IsCancelled);
            SelectAttendancesCommand = CommandsManager.CreateNotNull<Attendance>(x => Collection.Select(Items.Where(y => y.Attendance == x).ToList()), x => Wrappers.Any(y => y.Item.Attendance == x && !y.IsSelected));
            SetSelectedAttendancesToAbsentCommand = CommandsManager.Create(async () => await SetAttendancesAsync(SelectedItems, Attendance.Absent), () => SelectedItems.Any(x => x.Attendance != Attendance.Absent));
            SetSelectedAttendancesToPresentCommand = CommandsManager.Create(async () => await SetAttendancesAsync(SelectedItems, Attendance.Present), () => SelectedItems.Any(x => x.Attendance != Attendance.Present));
            SetSelectedAttendancesToInjuredCommand = CommandsManager.Create(async () => await SetAttendancesAsync(SelectedItems, Attendance.Injured), () => SelectedItems.Any(x => x.Attendance != Attendance.Injured));
            SetSelectedAttendancesToApologyCommand = CommandsManager.Create(async () => await SetAttendancesAsync(SelectedItems, Attendance.Apology), () => SelectedItems.Any(x => x.Attendance != Attendance.Apology));
            SetSelectedAttendancesToInSelectionCommand = CommandsManager.Create(async () => await SetAttendancesAsync(SelectedItems, Attendance.InSelection), () => SelectedItems.Any(x => x.Attendance != Attendance.InSelection));
            SetSelectedAttendancesToRestingCommand = CommandsManager.Create(async () => await SetAttendancesAsync(SelectedItems, Attendance.Resting), () => SelectedItems.Any(x => x.Attendance != Attendance.Resting));
            SetSelectedAttendancesToUnknownCommand = CommandsManager.Create(async () => await SetAttendancesAsync(SelectedItems, Attendance.Unknown), () => SelectedItems.Any(x => x.Attendance != Attendance.Unknown));
            SetSelectedAttendancesToInHolidaysCommand = CommandsManager.Create(async () => await SetAttendancesAsync(SelectedItems, Attendance.InHolidays), () => SelectedItems.Any(x => x.Attendance != Attendance.InHolidays));

            Disposables.AddRange(
            [
                trainingSessionChanged.Subscribe(x => currentSession = x),
                obs.Filter(x => x.Attendance == Attendance.Present).Count().Subscribe(x => CountPresents = x),
                obs.Filter(x => x.Attendance == Attendance.Absent).Count().Subscribe(x => CountAbsents = x),
                obs.Filter(x => x.Attendance == Attendance.Apology).Count().Subscribe(x => CountApologized = x),
                obs.Filter(x => x.Attendance == Attendance.Injured).Count().Subscribe(x => CountInjured = x),
                obs.Filter(x => x.Attendance == Attendance.InHolidays).Count().Subscribe(x => CountInHolidays = x),
                obs.Filter(x => x.Attendance == Attendance.InSelection).Count().Subscribe(x => CountInSelection = x),
                obs.Filter(x => x.Attendance == Attendance.Resting).Count().Subscribe(x => CountResting = x),
                obs.Filter(x => x.Attendance == Attendance.Unknown).Count().Subscribe(x => CountUnknown = x)
            ]);
        }

        public override async Task RemoveRangeAsync(IEnumerable<TrainingAttendanceViewModel> oldItems)
            => await _trainingSessionPresentationService.RemoveAttendancesAsync(oldItems).ConfigureAwait(false);

        private async Task SetAttendancesAsync(IEnumerable<TrainingAttendanceViewModel> items, Attendance attendance)
            => await _trainingSessionPresentationService.SetAttendancesAsync(items, attendance).ConfigureAwait(false);
    }
}

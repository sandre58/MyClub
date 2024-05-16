// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyClub.Teamup.Wpf.Services;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class TrainingAttendanceViewModel : EntityViewModelBase<TrainingAttendance>
    {
        private readonly TrainingSessionPresentationService _trainingSessionPresentationService;

        public TrainingAttendanceViewModel(TrainingAttendance item, TrainingSessionViewModel session, PlayerViewModel player, TrainingSessionPresentationService trainingSessionPresentationService) : base(item)
        {
            _trainingSessionPresentationService = trainingSessionPresentationService;
            Player = player;
            Session = session;

            SetAttendanceToAbsentCommand = CommandsManager.Create(async () => await SetAttendanceAsync(Attendance.Absent), () => Attendance != Attendance.Absent);
            SetAttendanceToPresentCommand = CommandsManager.Create(async () => await SetAttendanceAsync(Attendance.Present), () => Attendance != Attendance.Present);
            SetAttendanceToInjuredCommand = CommandsManager.Create(async () => await SetAttendanceAsync(Attendance.Injured), () => Attendance != Attendance.Injured);
            SetAttendanceToApologyCommand = CommandsManager.Create(async () => await SetAttendanceAsync(Attendance.Apology), () => Attendance != Attendance.Apology);
            SetAttendanceToInSelectionCommand = CommandsManager.Create(async () => await SetAttendanceAsync(Attendance.InSelection), () => Attendance != Attendance.InSelection);
            SetAttendanceToRestingCommand = CommandsManager.Create(async () => await SetAttendanceAsync(Attendance.Resting), () => Attendance != Attendance.Resting);
            SetAttendanceToUnknownCommand = CommandsManager.Create(async () => await SetAttendanceAsync(Attendance.Unknown), () => Attendance != Attendance.Unknown);
            SetAttendanceToInHolidaysCommand = CommandsManager.Create(async () => await SetAttendanceAsync(Attendance.InHolidays), () => Attendance != Attendance.InHolidays);

            Disposables.AddRange(
            [
                Player.WhenAnyPropertyChanged().Subscribe(_ => RaisePropertyChanged(nameof(Player)))
            ]);
        }

        public PlayerViewModel Player { get; }

        public TrainingSessionViewModel Session { get; }

        public Attendance Attendance => Item.Attendance;

        public double? Rating => Item.Rating;

        public string? Reason => Item.Reason;

        public string? Comment => Item.Comment;

        public ICommand SetAttendanceToUnknownCommand { get; }

        public ICommand SetAttendanceToPresentCommand { get; }

        public ICommand SetAttendanceToAbsentCommand { get; }

        public ICommand SetAttendanceToApologyCommand { get; }

        public ICommand SetAttendanceToInHolidaysCommand { get; }

        public ICommand SetAttendanceToInSelectionCommand { get; }

        public ICommand SetAttendanceToRestingCommand { get; }

        public ICommand SetAttendanceToInjuredCommand { get; }

        private async Task SetAttendanceAsync(Attendance attendance) => await _trainingSessionPresentationService.SetAttendancesAsync([this], attendance).ConfigureAwait(false);
    }
}

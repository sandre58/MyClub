// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData.Binding;
using MyClub.Teamup.Domain.Enums;
using MyNet.Observable;
using MyNet.Observable.Statistics;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class PlayerTrainingStatisticsViewModel : ObservableObject, IIdentifiable<Guid>
    {
        private readonly ObservableCollectionExtended<TrainingAttendanceViewModel> _attendances = [];
        private readonly ObservableCollectionExtended<TrainingAttendanceViewModel> _perfomedAttendances = [];
        private readonly ObservableCollectionExtended<TrainingAttendanceViewModel> _lastAttendances = [];

        public PlayerViewModel Player { get; }

        public Guid Id => Player.Id;

        public ReadOnlyObservableCollection<TrainingAttendanceViewModel> Attendances { get; protected set; }

        public ReadOnlyObservableCollection<TrainingAttendanceViewModel> PerformedAttendances { get; protected set; }

        public CountStatistics<TrainingAttendanceViewModel> Presents { get; } = new();

        public CountStatistics<TrainingAttendanceViewModel> Absents { get; } = new();

        public CountStatistics<TrainingAttendanceViewModel> AllAbsents { get; } = new();

        public CountStatistics<TrainingAttendanceViewModel> AllApologized { get; } = new();

        public CountStatistics<TrainingAttendanceViewModel> Apologized { get; } = new();

        public CountStatistics<TrainingAttendanceViewModel> InHolidays { get; } = new();

        public CountStatistics<TrainingAttendanceViewModel> InSelection { get; } = new();

        public CountStatistics<TrainingAttendanceViewModel> Injured { get; } = new();

        public CountStatistics<TrainingAttendanceViewModel> Resting { get; } = new();

        public CountStatistics<TrainingAttendanceViewModel> Unknown { get; } = new();

        public RangeStatistics<TrainingAttendanceViewModel> Ratings { get; } = new();

        public RangeStatistics<TrainingAttendanceViewModel> LastRatings { get; } = new();

        public uint CountLastAttendances { get; set; } = 5;

        public PlayerTrainingStatisticsViewModel(PlayerViewModel player)
        {
            Player = player;
            Attendances = new(_attendances);
            PerformedAttendances = new(_perfomedAttendances);
        }

        public void Refresh(IEnumerable<TrainingSessionViewModel> sessions)
        {
            _attendances.Set(sessions.SelectMany(x => x.Attendances).Where(x => x.Player.Id == Player.Id));
            _perfomedAttendances.Set(_attendances.Where(x => x.Session.IsPerformed).ToList());
            _lastAttendances.Set(sessions.Where(x => x.IsPerformed).OrderByDescending(x => x.StartDate).Take((int)CountLastAttendances).SelectMany(x => x.Attendances.Where(y => y.Player.Id == Player.Id).ToList()).ToList());

            Presents.Update(_perfomedAttendances, x => x.Attendance == Attendance.Present);
            Absents.Update(_perfomedAttendances, x => x.Attendance == Attendance.Absent);
            AllAbsents.Update(_perfomedAttendances, x => x.Attendance is not Attendance.Present and not Attendance.Unknown);
            AllApologized.Update(_perfomedAttendances, x => x.Attendance is not Attendance.Present and not Attendance.Absent and not Attendance.Unknown);
            Apologized.Update(_perfomedAttendances, x => x.Attendance == Attendance.Apology);
            InHolidays.Update(_perfomedAttendances, x => x.Attendance == Attendance.InHolidays);
            InSelection.Update(_perfomedAttendances, x => x.Attendance == Attendance.InSelection);
            Injured.Update(_perfomedAttendances, x => x.Attendance == Attendance.Injured);
            Resting.Update(_perfomedAttendances, x => x.Attendance == Attendance.Resting);
            Unknown.Update(_perfomedAttendances, x => x.Attendance == Attendance.Unknown);

            Ratings.Update(_perfomedAttendances, x => x.Attendance == Attendance.Present && x.Rating.HasValue, x => x.Rating!.Value);
            LastRatings.Update(_lastAttendances, x => x.Attendance == Attendance.Present && x.Rating.HasValue, x => x.Rating!.Value);

        }

        public override bool Equals(object? obj) => obj is PlayerTrainingStatisticsViewModel statisticsViewModel && Player.Equals(statisticsViewModel.Player);

        public override int GetHashCode() => Player.GetHashCode();
    }
}

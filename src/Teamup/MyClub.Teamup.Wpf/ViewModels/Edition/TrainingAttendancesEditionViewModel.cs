// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Observable.Collections;
using MyNet.Observable.Attributes;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class TrainingAttendancesEditionViewModel : EntityEditionViewModel<TrainingSession, TrainingSessionDto, TrainingSessionService>
    {
        private readonly TrainingSessionsProvider _trainingSessionsProvider;
        private readonly PlayersProvider _playersProvider;

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public TrainingSessionViewModel? Session { get; private set; }

        public ThreadSafeObservableCollection<EditableTrainingAttendanceViewModel> AvailablePlayers { get; private set; } = [];
        public ThreadSafeObservableCollection<EditableTrainingAttendanceViewModel> Presences { get; private set; } = [];
        public ThreadSafeObservableCollection<EditableTrainingAttendanceViewModel> Apologized { get; private set; } = [];
        public ThreadSafeObservableCollection<EditableTrainingAttendanceViewModel> Absents { get; private set; } = [];
        public ThreadSafeObservableCollection<EditableTrainingAttendanceViewModel> InSelection { get; private set; } = [];
        public ThreadSafeObservableCollection<EditableTrainingAttendanceViewModel> InHolidays { get; private set; } = [];
        public ThreadSafeObservableCollection<EditableTrainingAttendanceViewModel> Resting { get; private set; } = [];
        public ThreadSafeObservableCollection<EditableTrainingAttendanceViewModel> Unknown { get; private set; } = [];
        public ThreadSafeObservableCollection<EditableTrainingAttendanceViewModel> Injured { get; private set; } = [];
        public ThreadSafeObservableCollection<EditableTrainingAttendanceViewModel> AllAttendances { get; } = [];

        public ICommand SyncAbsencesCommand { get; set; }

        public ICommand ClearCommand { get; set; }

        public ICommand ClearListCommand { get; set; }

        public TrainingAttendancesEditionViewModel(
            TrainingSessionsProvider trainingSessionsProvider,
            TrainingSessionService trainingSessionService,
            PlayersProvider playersProvider)
            : base(trainingSessionService)
        {
            _trainingSessionsProvider = trainingSessionsProvider;
            _playersProvider = playersProvider;

            SyncAbsencesCommand = CommandsManager.Create(SyncAbsences);
            ClearCommand = CommandsManager.Create(Clear);
            ClearListCommand = CommandsManager.CreateNotNull<ObservableCollection<EditableTrainingAttendanceViewModel>>(ClearList);

            Disposables.AddRange([
                Presences.ToObservableChangeSet().OnItemAdded(x => x.Attendance = Attendance.Present).Subscribe(),
                Absents.ToObservableChangeSet().OnItemAdded(x => x.Attendance = Attendance.Absent).Subscribe(),
                Apologized.ToObservableChangeSet().OnItemAdded(x => x.Attendance = Attendance.Apology).Subscribe(),
                Injured.ToObservableChangeSet().OnItemAdded(x => x.Attendance = Attendance.Injured).Subscribe(),
                InHolidays.ToObservableChangeSet().OnItemAdded(x => x.Attendance = Attendance.InHolidays).Subscribe(),
                InSelection.ToObservableChangeSet().OnItemAdded(x => x.Attendance = Attendance.InSelection).Subscribe(),
                Resting.ToObservableChangeSet().OnItemAdded(x => x.Attendance = Attendance.Resting).Subscribe(),
                Unknown.ToObservableChangeSet().OnItemAdded(x => x.Attendance = Attendance.Unknown).Subscribe(),
                Presences.ToObservableChangeSet().Merge(Unknown.ToObservableChangeSet())
                                                 .Merge(Absents.ToObservableChangeSet())
                                                 .Merge(Apologized.ToObservableChangeSet())
                                                 .Merge(Injured.ToObservableChangeSet())
                                                 .Merge(InHolidays.ToObservableChangeSet())
                                                 .Merge(InSelection.ToObservableChangeSet())
                                                 .Merge(Resting.ToObservableChangeSet())
                                                 .OnItemAdded(x => AllAttendances.Contains(x).IfFalse(() => AllAttendances.Add(x)))
                                                 .OnItemRemoved(x => AllAttendances.Remove(x))
                                                 .Subscribe()
            ]);
        }

        private void Clear()
        {
            ClearList(Presences);
            ClearList(Absents);
            ClearList(Apologized);
            ClearList(Injured);
            ClearList(InHolidays);
            ClearList(InSelection);
            ClearList(Resting);
            ClearList(Unknown);
        }

        private void ClearList(ObservableCollection<EditableTrainingAttendanceViewModel> list) => MoveTo(list, AvailablePlayers);

        private void SyncAbsences()
        {
            if (Session is null) return;

            var allEditableItems = AllAttendances.Union(AvailablePlayers).Where(x => Session.Teams.Contains(x.Player.Team)).ToList();
            allEditableItems.Where(x => x.Player.IsAbsentAtDate(Session.StartDate)).ForEach(x =>
            {
                switch (x.Player.Absences.FirstOrDefault(y => y.ContainsDate(Session.StartDate))?.Type.ToAttendance())
                {
                    case Attendance.InSelection:
                        MoveTo([x], InSelection);
                        break;
                    case Attendance.InHolidays:
                        MoveTo([x], InHolidays);
                        break;
                    case Attendance.Apology:
                        MoveTo([x], Apologized);
                        break;
                    default:
                        MoveTo([x], Unknown);
                        break;
                }
            });

            MoveTo(allEditableItems.Where(x => x.Player.IsInjuredAtDate(Session.StartDate)).ToList(), Injured);
        }

        private void MoveTo(IEnumerable<EditableTrainingAttendanceViewModel> items, ICollection<EditableTrainingAttendanceViewModel> list)
        {
            var itemsToMove = items.ToList();
            AvailablePlayers.RemoveMany(itemsToMove);
            Presences.RemoveMany(itemsToMove);
            Absents.RemoveMany(itemsToMove);
            Apologized.RemoveMany(itemsToMove);
            Injured.RemoveMany(itemsToMove);
            InHolidays.RemoveMany(itemsToMove);
            InSelection.RemoveMany(itemsToMove);
            Resting.RemoveMany(itemsToMove);
            Unknown.RemoveMany(itemsToMove);

            list.AddRange(itemsToMove);
        }

        protected override TrainingSessionDto ToDto()
        {
            if (Session is null) throw new InvalidOperationException("Training session cannot be null");

            var dto = new TrainingSessionDto
            {
                Id = ItemId,
                Place = Session.Place,
                Theme = Session.Theme,
                IsCancelled = Session.IsCancelled,
                StartDate = Session.StartDate,
                EndDate = Session.EndDate,
                Attendances = AllAttendances.Select(x => new TrainingAttendanceDto
                {
                    Id = x.Id,
                    Attendance = x.Attendance,
                    Comment = x.Comment,
                    PlayerId = x.Player.PlayerId,
                    Rating = x.Attendance == Attendance.Present ? x.Rating.Value : null,
                    Reason = x.Attendance == Attendance.Apology ? x.Reason : null
                }).ToList()
            };

            return dto;
        }

        protected override void RefreshFrom(TrainingSession item)
        {
            Session = _trainingSessionsProvider.GetOrThrow(item.Id);
            Clear();
            AvailablePlayers.Set(_playersProvider.Items.Select(x => new EditableTrainingAttendanceViewModel(x, Attendance.Unknown)));

            var attendancePlayers = AvailablePlayers.Where(x => item.Attendances.Any(y => y.Player.Id == x.Player.PlayerId)).Select(x =>
            {
                var playerAttendance = item.Attendances.First(y => y.Player.Id == x.Player.PlayerId);
                x.Attendance = playerAttendance.Attendance;
                x.Comment = playerAttendance.Comment;
                x.Rating.Value = playerAttendance.Rating;
                x.Reason = playerAttendance.Reason;

                return x;
            }).ToList();

            MoveTo(attendancePlayers.Where(x => x.Attendance == Attendance.Present).ToList(), Presences);
            MoveTo(attendancePlayers.Where(x => x.Attendance == Attendance.Apology).ToList(), Apologized);
            MoveTo(attendancePlayers.Where(x => x.Attendance == Attendance.Absent).ToList(), Absents);
            MoveTo(attendancePlayers.Where(x => x.Attendance == Attendance.InSelection).ToList(), InSelection);
            MoveTo(attendancePlayers.Where(x => x.Attendance == Attendance.InHolidays).ToList(), InHolidays);
            MoveTo(attendancePlayers.Where(x => x.Attendance == Attendance.Resting).ToList(), Resting);
            MoveTo(attendancePlayers.Where(x => x.Attendance == Attendance.Injured).ToList(), Injured);
            MoveTo(attendancePlayers.Where(x => x.Attendance == Attendance.Unknown).ToList(), Unknown);
        }

        protected override void ResetItem() { }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SquadAggregate;

namespace MyClub.Teamup.Domain.TrainingAggregate
{
    public class TrainingSession : TrainingSessionBase
    {
        private readonly ObservableCollection<TrainingAttendance> _attendances = [];
        private readonly ObservablePeriod _period;

        public TrainingSession(DateTime start, DateTime end, Guid? id = null) : base(string.Empty, id)
        {
            _period = new(start, end);
            Attendances = new(_attendances);
        }

        public override string Theme { get; set; } = string.Empty;

        public string? Place { get; set; }

        public DateTime Start => _period.Start;

        public DateTime End => _period.End;

        public virtual bool IsCancelled { get; private set; }

        public ObservableCollection<Guid> TeamIds { get; } = [];

        public ReadOnlyObservableCollection<TrainingAttendance> Attendances { get; }

        public void Cancel()
        {
            IsCancelled = true;
            _attendances.Clear();
        }

        public void UndoCancel() => IsCancelled = false;

        public void SetDate(DateTime start, DateTime end)
        {
            var oldStartDate = _period.Start;
            var oldEndDate = _period.End;
            _period.SetInterval(start, end);
            Attendances.Where(x => Start.IsInFuture()).ForEach(x => x.ResetCommentAndRating());

            if (oldStartDate != start)
                RaisePropertyChanged(nameof(Start));

            if (oldEndDate != end)
                RaisePropertyChanged(nameof(End));
        }

        public TrainingAttendance AddAttendance(Player player)
        {
            var absence = player.Absences.LastOrDefault(x => x.Period.Contains(Start));
            var attendance = player.IsInjuredAtDate(Start) ? Attendance.Injured : absence is not null ? absence.Type.ToAttendance() : Attendance.Unknown;

            return AddAttendance(player, attendance, reason: absence?.Label);
        }

        public TrainingAttendance AddAttendance(Player player, Attendance attendance, double? rating = null, string? reason = null, string? comment = null)
        {
            var trainingAttendance = new TrainingAttendance(player, attendance, rating)
            {
                Reason = attendance == Attendance.Apology ? reason : null,
                Comment = comment
            };

            return AddAttendance(trainingAttendance);
        }

        public TrainingAttendance AddAttendance(TrainingAttendance trainingAttendance)
        {
            _attendances.Add(trainingAttendance);

            return trainingAttendance;
        }

        public IEnumerable<TrainingAttendance> InitializeAttendances(IEnumerable<SquadPlayer> players)
        {
            var playerAlreadyAdded = Attendances.Select(x => x.Player).ToList();
            var playersToAdd = players.Where(x => x.Team is not null && TeamIds.Contains(x.Team.Id) && !playerAlreadyAdded.Contains(x.Id)).ToList();
            return playersToAdd.Select(x => AddAttendance(x.Player)).ToList();
        }

        public bool RemoveAttendance(Guid playerId)
            => _attendances.Any(x => x.Player.Id == playerId) && _attendances.Remove(_attendances.First(x => x.Player.Id == playerId));

        public bool RemoveAttendance(IEnumerable<Guid> playerIds) => playerIds.All(RemoveAttendance);

        public override string? ToString() => _period.ToString();
    }
}

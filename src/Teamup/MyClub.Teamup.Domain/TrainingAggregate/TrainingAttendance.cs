// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyNet.Utilities.Sequences;

namespace MyClub.Teamup.Domain.TrainingAggregate
{
    public class TrainingAttendance : Entity
    {

        public static readonly AcceptableValueRange<double> AcceptableRangeRating = new(0, 10);

        private Attendance _attendance;
        private double? _rating;

        public TrainingAttendance(Player player, Attendance attendance = Attendance.Unknown, double? rating = null, Guid? id = null) : base(id)
        {
            Player = player;
            Rating = rating;
            Attendance = attendance;
        }

        public Player Player { get; }

        public Attendance Attendance
        {
            get => _attendance;
            set
            {
                _attendance = value;

                if (_attendance != Attendance.Present)
                    ResetCommentAndRating();
            }
        }

        public string? Reason { get; set; }

        public double? Rating
        {
            get => _rating;
            set => _rating = AcceptableRangeRating.ValidateOrThrow(value);
        }

        public string? Comment { get; set; }

        public void ResetCommentAndRating()
        {
            Rating = null;
            Comment = null;
        }

        public override string ToString() => $"{Player} - {Attendance}";
    }
}

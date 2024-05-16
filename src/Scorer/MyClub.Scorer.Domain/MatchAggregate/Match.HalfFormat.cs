// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class HalfFormat : ValueObject
    {
        public static readonly HalfFormat Default = new(2, 45.Minutes(), 15.Minutes());

        public static readonly HalfFormat ExtraTime = new(2, 15.Minutes(), 5.Minutes());

        public HalfFormat(int number, TimeSpan duration, TimeSpan? halfTimeDuration = null)
        {
            Number = number;
            Duration = duration;
            HalfTimeDuration = halfTimeDuration;
        }

        public int Number { get; }

        public TimeSpan Duration { get; }

        public TimeSpan? HalfTimeDuration { get; }

        public TimeSpan GetFullTime(bool withHalfTime = true) => Number * Duration + (Number - 1) * (withHalfTime && HalfTimeDuration.HasValue ? HalfTimeDuration.Value : TimeSpan.Zero);

        public override string ToString() => $"{Number}x{Duration.TotalMinutes}'";
    }
}

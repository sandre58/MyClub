// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;

namespace MyClub.Teamup.Domain.MatchAggregate
{
    public class HalfFormat : ValueObject
    {
        public static readonly HalfFormat Default = new(2, 45.Minutes());

        public static readonly HalfFormat ExtraTime = new(2, 15.Minutes());

        public HalfFormat(int number, TimeSpan duration)
        {
            Number = number;
            Duration = duration;
        }

        public int Number { get; }

        public TimeSpan Duration { get; }

        public TimeSpan GetFullTime() => Number * Duration;

        public override string ToString() => $"{Number}x{Duration.TotalMinutes}'";
    }
}

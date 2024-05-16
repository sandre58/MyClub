// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Teamup.Domain.SeasonAggregate
{
    public class Season : LabelEntity, IAggregateRoot, IHasPeriod
    {
        public static readonly Season CurrentYear = new(DateTime.Today.BeginningOfYear(), DateTime.Today.EndOfYear());

        public static readonly Season Middle = new(DateTime.Today.AddMonths(-6), DateTime.Today.AddMonths(6));
        public static readonly Season Current = new(DateTime.Today.Previous(1, 7), DateTime.Today.Next(30, 6));

        public Season(DateTime startDate, DateTime endDate, Guid? id = null) : base($"{startDate.Year} - {endDate.Year}", id) => Period = new ObservablePeriod(startDate.BeginningOfDay(), endDate.EndOfDay());

        public ObservablePeriod Period { get; }

        public override int CompareTo(object? obj)
        {
            if (obj is Season other)
            {
                var value = base.CompareTo(other);

                return value != 0 ? value : Period.CompareTo(other.Period);
            }
            return 1;
        }

        public string GetShortName() => Period.Start.Year == Period.End.Year ? Period.Start.ToString("yy") : $"{Period.Start:yy}/{Period.End:yy}";

        public override string ToString() => Period.Start.Year == Period.End.Year ? Period.Start.Year.ToString() : $"{Period.Start.Year}/{Period.End.Year}";
    }
}

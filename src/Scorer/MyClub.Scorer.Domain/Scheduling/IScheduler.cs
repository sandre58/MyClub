// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.Scheduling
{
    public interface IScheduler<T>
    {
        void Schedule(IEnumerable<T> items);
    }

    public interface IDateScheduler<T> : IScheduler<T> where T : ISchedulable
    {
        DateTime GetFromDate();

        void Reset(DateTime fromDate, IEnumerable<T>? scheduledItems = null);
    }

    public interface IVenueScheduler : IScheduler<Match>
    {
    }
}


﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.Scheduling
{
    public interface IScheduler<T> where T : ISchedulable
    {
        void Schedule(IEnumerable<T> items);
    }

    public interface IMatchesScheduler : IScheduler<Match>
    {
    }
}


// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Domain.Scheduling
{
    public interface IAvailableDateSchedulingRule
    {
        IEnumerable<Period> GetAvailablePeriods(Period period);

        DateTime GetNextAvailableDate(Period matchPeriod);
    }
}


// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MyClub.Scorer.Domain.Scheduling
{
    public interface IDateSchedulingRule
    {
        IEnumerable<IDateSchedulingRule> ConvertToTimeZone(TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone);

        bool Match(DateOnly date, DateOnly? previousDate);
    }
}

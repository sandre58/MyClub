// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Scorer.Domain.Scheduling
{
    public interface ITimeSchedulingRule
    {
        TimeOnly? ProvideTime(DateOnly date, int index);
    }
}

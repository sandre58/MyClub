// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Domain.Extensions
{
    public static class MatchExtensions
    {
        public static SchedulingParameters GetSchedulingParameters(this Match match) => match.Parent.ProvideSchedulingParameters();

        public static TimeSpan GetRotationTime(this Match match) => match.GetSchedulingParameters().RotationTime;

        public static TimeSpan GetRestTime(this Match match) => match.GetSchedulingParameters().RestTime;
    }
}

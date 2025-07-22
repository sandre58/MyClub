// -----------------------------------------------------------------------
// <copyright file="MatchFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

public record MatchFormat(PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, int? NumberOfPenaltyShootouts = null)
{
    public static readonly MatchFormat Default = new(PeriodFormat.Default);

    public static readonly MatchFormat NoDraw = new(PeriodFormat.Default, PeriodFormat.ExtraTime, 5);

    public bool ExtraTimeIsEnabled => ExtraTime is not null;

    public bool ShootoutIsEnabled => NumberOfPenaltyShootouts > 0;

    public TimeSpan GetFullTime(bool withHalfTime = true) => RegulationTime.GetFullTime(withHalfTime) + (ExtraTime?.GetFullTime(withHalfTime) ?? TimeSpan.Zero);

    public override string ToString()
    {
        var str = new StringBuilder(RegulationTime.ToString());

        if (ExtraTimeIsEnabled)
            _ = str.Append(CultureInfo.CurrentCulture, $" ({ExtraTime})");

        if (ShootoutIsEnabled)
            _ = str.Append(CultureInfo.CurrentCulture, $" + {NumberOfPenaltyShootouts} penalty shootouts");

        return str.ToString();
    }
}

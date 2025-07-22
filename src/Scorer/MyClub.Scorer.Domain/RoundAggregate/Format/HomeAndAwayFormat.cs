// -----------------------------------------------------------------------
// <copyright file="HomeAndAwayFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

namespace MyClub.Scorer.Domain.RoundAggregate.Format;

public record HomeAndAwayFormat(PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, bool UseAwayGoals = false, int? NumberOfPenaltyShootouts = null) : RoundFormat(RegulationTime, ExtraTime, NumberOfPenaltyShootouts)
{
    public override bool AllowDraw() => true;
}

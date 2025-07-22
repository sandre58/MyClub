// -----------------------------------------------------------------------
// <copyright file="StandingCalculator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Shared.Domain.Standings.Services;

public class StandingCalculator : IStandingCalculator
{
    public Standing Calculate(IEnumerable<TeamReference> teams, IEnumerable<IMatch> matches, StandingRuleSet rules, IReadOnlyDictionary<TeamReference, int>? penaltyPoints = null)
    {
        var standing = new Standing(teams, rules, penaltyPoints);
        standing.ComputeAll(matches);

        return standing;
    }
}

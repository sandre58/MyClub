// -----------------------------------------------------------------------
// <copyright file="IChampionship.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Domain.Primitives;

public interface IChampionship : ITeamsContainer
{
    StandingRankStatuses StandingRankStatuses { get; }

    StandingRuleSet StandingRules { get; }

    IReadOnlyCollection<MatchdayId> Matchdays { get; }

    IReadOnlyDictionary<TeamId, int> GetPenaltyPoints();
}

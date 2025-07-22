// -----------------------------------------------------------------------
// <copyright file="IMatchdayScheduleStrategy.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Domain.MatchdayAggregate.Services;

public interface IMatchdayScheduleStrategy
{
    IEnumerable<MatchdayDefinition> GenerateSchedule(IEnumerable<TeamReference> teams);

    int GetMatchdayCount(int teamCount);

    int GetMaxMatchesPerMatchday(int teamCount);
}

public record MatchdayDefinition(IReadOnlyCollection<FixtureDefinition> Fixtures);

public record FixtureDefinition(TeamReference HomeTeam, TeamReference AwayTeam);

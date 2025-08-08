// -----------------------------------------------------------------------
// <copyright file="LeagueMatchday.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Shared.Infrastructure.Persistence.JoinEntities;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Join entity representing the many-to-many relationship between League entities and Matchday entities.
/// This entity enables league competitions to organize matches into structured rounds or game weeks
/// while maintaining proper Entity Framework Core relationship mapping.
/// </summary>
/// <param name="entity">The league competition that organizes the matchdays.</param>
/// <param name="matchday">The matchday that belongs to the league.</param>
/// <remarks>
/// The LeagueMatchday join entity is fundamental to league-based competition organization,
/// enabling traditional round-robin league formats where all teams play each other over
/// a series of organized matchdays throughout the competition season.
/// </remarks>
internal sealed class LeagueMatchday(League entity, Matchday matchday) : EntityLink<League, Matchday, CompetitionId, MatchdayId>(entity, matchday, entity.Id, matchday.Id)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private LeagueMatchday()
        : this(null!, null!) { }
}

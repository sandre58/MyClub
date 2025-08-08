// -----------------------------------------------------------------------
// <copyright file="MatchdayMatch.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.MatchdayAggregate;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Join entity representing the many-to-many relationship between Matchday entities and Match entities.
/// This entity organizes individual matches within specific matchdays, enabling structured scheduling
/// and organization of football fixtures across competition rounds.
/// </summary>
/// <param name="matchday">The matchday that contains and organizes the matches.</param>
/// <param name="match">The individual match assigned to the matchday.</param>
internal sealed class MatchdayMatch(Matchday matchday, Match match) : EntityMatch<Matchday, MatchdayId>(matchday, match)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private MatchdayMatch()
        : this(null!, null!)
    {
    }
}

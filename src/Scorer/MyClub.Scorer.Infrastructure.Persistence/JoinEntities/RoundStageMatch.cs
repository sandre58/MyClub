// -----------------------------------------------------------------------
// <copyright file="RoundStageMatch.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.RoundAggregate;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Join entity representing the many-to-many relationship between RoundStage entities and Match entities.
/// This entity organizes matches within specific round stages, enabling complex knockout tournament
/// structures with multiple legs or phases within each round.
/// </summary>
/// <param name="roundStage">The round stage that contains the matches.</param>
/// <param name="match">The match assigned to the round stage.</param>
/// <remarks>
/// The RoundStageMatch join entity enables sophisticated round organization where each round
/// can be divided into multiple stages (such as first leg, second leg, or multiple games
/// in best-of series), providing precise control over match scheduling and progression.
/// </remarks>
internal sealed class RoundStageMatch(RoundStage roundStage, Match match) : EntityMatch<RoundStage, RoundStageId>(roundStage, match)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private RoundStageMatch()
        : this(null!, null!)
    {
    }
}

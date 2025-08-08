// -----------------------------------------------------------------------
// <copyright file="CupRound.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Shared.Infrastructure.Persistence.JoinEntities;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Join entity representing the many-to-many relationship between Cup competitions and Round entities.
/// This entity enables cup competitions to contain multiple elimination rounds while maintaining
/// proper Entity Framework Core relationship mapping and referential integrity.
/// </summary>
/// <param name="entity">The cup competition that contains the round.</param>
/// <param name="round">The round that belongs to the cup competition.</param>
internal sealed class CupRound(Cup entity, Round round) : EntityLink<Cup, Round, CompetitionId, RoundId>(entity, round, entity.Id, round.Id)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private CupRound()
        : this(null!, null!) { }
}

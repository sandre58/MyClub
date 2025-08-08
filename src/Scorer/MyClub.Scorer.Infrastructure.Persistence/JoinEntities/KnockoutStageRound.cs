// -----------------------------------------------------------------------
// <copyright file="KnockoutStageRound.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Infrastructure.Persistence.JoinEntities;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Join entity representing the many-to-many relationship between KnockoutStage entities and Round entities.
/// This entity enables knockout stages to contain multiple elimination rounds while maintaining
/// proper Entity Framework Core relationship mapping and tournament progression flow.
/// </summary>
/// <param name="entity">The knockout stage that contains the rounds.</param>
/// <param name="round">The round that belongs to the knockout stage.</param>
/// <remarks>
/// The KnockoutStageRound join entity facilitates elimination tournament organization within
/// larger tournament structures, enabling sophisticated knockout phases with multiple rounds
/// of elimination and team progression based on match results.
/// </remarks>
internal sealed class KnockoutStageRound(KnockoutStage entity, Round round) : EntityLink<KnockoutStage, Round, StageId, RoundId>(entity, round, entity.Id, round.Id)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private KnockoutStageRound()
        : this(null!, null!) { }
}

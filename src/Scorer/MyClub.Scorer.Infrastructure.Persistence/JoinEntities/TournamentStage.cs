// -----------------------------------------------------------------------
// <copyright file="TournamentStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Infrastructure.Persistence.JoinEntities;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Join entity representing the many-to-many relationship between Tournament entities and Stage entities.
/// This entity enables tournaments to contain multiple competition stages while maintaining
/// proper Entity Framework Core relationship mapping and tournament progression logic.
/// </summary>
/// <param name="entity">The tournament that contains the stages.</param>
/// <param name="stage">The stage that belongs to the tournament.</param>
/// <remarks>
/// The TournamentStage join entity facilitates complex multi-stage tournament organization,
/// enabling sophisticated tournament formats that combine different competition types
/// such as group stages followed by knockout phases within a single tournament structure.
/// </remarks>
internal sealed class TournamentStage(Tournament entity, Stage stage) : EntityLink<Tournament, Stage, CompetitionId, StageId>(entity, stage, entity.Id, stage.Id)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private TournamentStage()
        : this(null!, null!) { }
}

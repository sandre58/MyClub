// -----------------------------------------------------------------------
// <copyright file="ChampionshipStageMatchday.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Infrastructure.Persistence.JoinEntities;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Join entity representing the many-to-many relationship between ChampionshipStage entities and Matchday entities.
/// This entity enables championship stages to organize matches into structured rounds or game weeks
/// while maintaining proper Entity Framework Core relationship mapping.
/// </summary>
/// <param name="entity">The championship stage that organizes the matchdays.</param>
/// <param name="matchday">The matchday that belongs to the championship stage.</param>
internal sealed class ChampionshipStageMatchday(ChampionshipStage entity, Matchday matchday) : EntityLink<ChampionshipStage, Matchday, StageId, MatchdayId>(entity, matchday, entity.Id, matchday.Id)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private ChampionshipStageMatchday()
        : this(null!, null!) { }
}

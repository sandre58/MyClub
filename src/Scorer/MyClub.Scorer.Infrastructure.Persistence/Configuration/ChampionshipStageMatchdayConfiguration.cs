// -----------------------------------------------------------------------
// <copyright file="ChampionshipStageMatchdayConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for ChampionshipStageMatchday join entities,
/// establishing the many-to-many relationship mapping between ChampionshipStage entities
/// and Matchday entities for league-style tournament phase organization.
/// </summary>
/// <remarks>
/// The ChampionshipStageMatchdayConfiguration provides standardized join entity
/// configuration for championship stages containing organized matchdays, enabling
/// league-style tournament phases with structured round organization similar to
/// traditional league formats within larger tournament contexts.
/// </remarks>
internal sealed class ChampionshipStageMatchdayConfiguration : IEntityTypeConfiguration<ChampionshipStageMatchday>
{
    /// <summary>
    /// Configures the ChampionshipStageMatchday join entity using the standardized
    /// join entity configuration pattern for ChampionshipStage-Matchday relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for ChampionshipStageMatchday configuration.</param>
    /// <remarks>
    /// This configuration applies the standard join entity pattern establishing composite
    /// primary keys, foreign key relationships with appropriate cascade behaviors, and
    /// index optimization for efficient matchday retrieval by championship stage while
    /// supporting structured round organization for league-style tournament phases.
    /// </remarks>
    public void Configure(EntityTypeBuilder<ChampionshipStageMatchday> builder) => builder.ConfigureJoinEntity<ChampionshipStageMatchday, ChampionshipStage, Matchday, StageId, MatchdayId>();
}

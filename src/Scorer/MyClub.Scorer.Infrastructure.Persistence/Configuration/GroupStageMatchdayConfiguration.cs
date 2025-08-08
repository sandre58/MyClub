// -----------------------------------------------------------------------
// <copyright file="GroupStageMatchdayConfiguration.cs" company="Stéphane ANDRE">
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
/// Entity Framework Core configuration for GroupStageMatchday join entities, establishing
/// the many-to-many relationship mapping between GroupStage entities and Matchday entities
/// for group-based tournament phase organization with structured round scheduling.
/// </summary>
/// <remarks>
/// The GroupStageMatchdayConfiguration provides standardized join entity configuration
/// for group stages containing organized matchdays, enabling group-based tournament phases
/// with structured round organization where teams within groups compete in round-robin
/// formats across multiple organized matchdays within tournament structures.
/// </remarks>
internal sealed class GroupStageMatchdayConfiguration : IEntityTypeConfiguration<GroupStageMatchday>
{
    /// <summary>
    /// Configures the GroupStageMatchday join entity using the standardized join entity
    /// configuration pattern for GroupStage-Matchday relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for GroupStageMatchday configuration.</param>
    /// <remarks>
    /// This configuration applies the standard join entity pattern establishing composite
    /// primary keys, foreign key relationships with appropriate cascade behaviors, and
    /// index optimization for efficient matchday retrieval by group stage while supporting
    /// structured round organization for group-based tournament competition phases.
    /// </remarks>
    public void Configure(EntityTypeBuilder<GroupStageMatchday> builder) => builder.ConfigureJoinEntity<GroupStageMatchday, GroupStage, Matchday, StageId, MatchdayId>();
}

// -----------------------------------------------------------------------
// <copyright file="StageTeamConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for StageTeam join entities, establishing
/// the many-to-many relationship mapping between Stage entities and team references
/// for multi-stage tournament organization and team progression management.
/// </summary>
/// <remarks>
/// The StageTeamConfiguration provides standardized join entity configuration for
/// stages containing team assignments, enabling multi-stage tournament structures
/// where teams progress between different tournament phases based on results and
/// qualification criteria while supporting both concrete and virtual team references.
/// </remarks>
internal sealed class StageTeamConfiguration : IEntityTypeConfiguration<StageTeam>
{
    /// <summary>
    /// Configures the StageTeam join entity using the standardized join team
    /// configuration pattern for Stage-Team relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for StageTeam configuration.</param>
    /// <remarks>
    /// This configuration applies the standard join team pattern establishing composite
    /// primary keys, foreign key relationships, team reference conversion, and index
    /// optimization for efficient team retrieval by stage while supporting polymorphic
    /// team references for complex tournament progression scenarios.
    /// </remarks>
    public void Configure(EntityTypeBuilder<StageTeam> builder) => builder.ConfigureJoinTeam<StageTeam, Stage, StageId>();
}

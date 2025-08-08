// -----------------------------------------------------------------------
// <copyright file="KnockoutStageRoundConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for KnockoutStageRound join entities, establishing
/// the many-to-many relationship mapping between KnockoutStage entities and Round entities
/// for elimination tournament phase organization with structured knockout progression.
/// </summary>
/// <remarks>
/// The KnockoutStageRoundConfiguration provides standardized join entity configuration
/// for knockout stages containing elimination rounds, enabling elimination tournament
/// phases with structured round progression where teams advance through multiple
/// knockout phases within larger tournament elimination structures.
/// </remarks>
internal sealed class KnockoutStageRoundConfiguration : IEntityTypeConfiguration<KnockoutStageRound>
{
    /// <summary>
    /// Configures the KnockoutStageRound join entity using the standardized join entity
    /// configuration pattern for KnockoutStage-Round relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for KnockoutStageRound configuration.</param>
    /// <remarks>
    /// This configuration applies the standard join entity pattern establishing composite
    /// primary keys, foreign key relationships with appropriate cascade behaviors, and
    /// index optimization for efficient round retrieval by knockout stage while supporting
    /// complex elimination tournament structures with multiple knockout phases.
    /// </remarks>
    public void Configure(EntityTypeBuilder<KnockoutStageRound> builder) => builder.ConfigureJoinEntity<KnockoutStageRound, KnockoutStage, Round, StageId, RoundId>();
}

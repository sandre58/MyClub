// -----------------------------------------------------------------------
// <copyright file="KnockoutStageConfiguration.cs" company="Stéphane ANDRE">
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
/// Entity Framework Core configuration for KnockoutStage entities, establishing the
/// many-to-many relationship with Round entities for elimination tournament phase
/// organization with multiple knockout rounds and elimination progression.
/// </summary>
/// <remarks>
/// The KnockoutStageConfiguration extends the base Stage configuration with knockout-specific
/// round relationship management, enabling elimination tournament phases with multiple
/// knockout rounds where teams progress through structured elimination until determining
/// winners within tournament knockout phase structures.
/// </remarks>
internal sealed class KnockoutStageConfiguration : IEntityTypeConfiguration<KnockoutStage>
{
    /// <summary>
    /// Configures the KnockoutStage entity with round relationship management using the
    /// standardized many-to-many join entity pattern for elimination tournaments.
    /// </summary>
    /// <param name="builder">The entity type builder for KnockoutStage configuration.</param>
    /// <remarks>
    /// This configuration establishes the KnockoutStage-Round many-to-many relationship
    /// through the KnockoutStageRound join entity, enabling knockout stages to contain
    /// multiple elimination rounds with different formats and team progression patterns
    /// while maintaining referential integrity and supporting complex elimination scenarios.
    /// </remarks>
    public void Configure(EntityTypeBuilder<KnockoutStage> builder) => builder.HasMany<KnockoutStageRound, KnockoutStage, Round, StageId, RoundId>();
}

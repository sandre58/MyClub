// -----------------------------------------------------------------------
// <copyright file="GroupStageConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Infrastructure.Persistence.Converters;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for GroupStage entities, establishing comprehensive
/// mapping for group stage data including group management, standing calculations, penalty
/// point tracking, and matchday organization for round-robin tournament phases.
/// </summary>
/// <remarks>
/// The GroupStageConfiguration extends the base Stage configuration with group-specific
/// features including group collection management, standing rules and labels, penalty
/// point systems, and matchday organization patterns required for tournament phases
/// where teams are organized into groups for round-robin competition formats.
/// </remarks>
internal sealed class GroupStageConfiguration : IEntityTypeConfiguration<GroupStage>
{
    /// <summary>
    /// Configures the GroupStage entity with group-specific properties including group
    /// relationships, standing management, penalty point tracking, and matchday organization.
    /// </summary>
    /// <param name="builder">The entity type builder for GroupStage configuration.</param>
    /// <remarks>
    /// This configuration establishes group relationships with cascade deletion, matchday
    /// relationships through join entities, penalty points dictionary with team ID mapping,
    /// standing labels ownership for group table presentation, and standing rules ownership
    /// for group calculation logic supporting complex group stage tournament scenarios.
    /// </remarks>
    public void Configure(EntityTypeBuilder<GroupStage> builder)
    {
        builder.HasMany(static x => x.Groups).WithOne().OnDelete(DeleteBehavior.Cascade);
        builder.HasMany<GroupStageMatchday, GroupStage, Matchday, StageId, MatchdayId>();
        builder.Property(static x => x.PenaltyPoints).HasColumnName(nameof(GroupStage.PenaltyPoints)).HasConversion(new DictionaryConverter<TeamId, int>());
        builder.OwnsManyStandingLabels(static x => x.Labels);
        builder.OwnsStandingRules(static x => x.StandingRules);
    }
}

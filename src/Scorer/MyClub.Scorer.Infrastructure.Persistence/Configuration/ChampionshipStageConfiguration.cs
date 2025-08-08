// -----------------------------------------------------------------------
// <copyright file="ChampionshipStageConfiguration.cs" company="Stéphane ANDRE">
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
/// Entity Framework Core configuration for ChampionshipStage entities, establishing
/// comprehensive mapping for championship-specific data including standing management,
/// penalty point tracking, and matchday organization for league-style tournament phases.
/// </summary>
/// <remarks>
/// The ChampionshipStageConfiguration extends the base Stage configuration with
/// championship-specific features including standing rules and labels, penalty point
/// management, and matchday organization patterns required for league-style phases
/// within larger tournament structures where teams compete in round-robin formats.
/// </remarks>
internal sealed class ChampionshipStageConfiguration : IEntityTypeConfiguration<ChampionshipStage>
{
    /// <summary>
    /// Configures the ChampionshipStage entity with championship-specific properties
    /// including standing management, penalty point tracking, and matchday relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for ChampionshipStage configuration.</param>
    /// <remarks>
    /// This configuration establishes matchday relationships through join entities,
    /// standing labels ownership for championship table presentation, standing rules
    /// ownership for championship calculation logic, and penalty points dictionary
    /// with team ID mapping for disciplinary point management within championship phases.
    /// </remarks>
    public void Configure(EntityTypeBuilder<ChampionshipStage> builder)
    {
        builder.HasMany<ChampionshipStageMatchday, ChampionshipStage, Matchday, StageId, MatchdayId>();
        builder.OwnsManyStandingLabels(static x => x.Labels);
        builder.OwnsStandingRules(static x => x.StandingRules);
        builder.Property(static x => x.PenaltyPoints).HasColumnName(nameof(ChampionshipStage.PenaltyPoints)).HasConversion(new DictionaryConverter<TeamId, int>());
    }
}

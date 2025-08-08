// -----------------------------------------------------------------------
// <copyright file="LeagueConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Infrastructure.Persistence.Converters;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for League entities, establishing comprehensive
/// mapping for league-specific data including standing management, penalty point tracking,
/// and matchday organization for round-robin competition formats.
/// </summary>
/// <remarks>
/// The LeagueConfiguration extends the base Competition configuration with league-specific
/// features including standing rules and labels, penalty point management, and matchday
/// organization patterns required for traditional round-robin league competitions where
/// all teams play each other over a structured season format.
/// </remarks>
internal sealed class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    /// <summary>
    /// Configures the League entity with league-specific properties including standing
    /// management, penalty point tracking, and matchday relationship configuration.
    /// </summary>
    /// <param name="builder">The entity type builder for League configuration.</param>
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.HasMany<LeagueMatchday, League, Matchday, CompetitionId, MatchdayId>();
        builder.OwnsManyStandingLabels(static x => x.Labels);
        builder.OwnsStandingRules(static x => x.StandingRules);
        builder.Property(static x => x.PenaltyPoints).HasConversion(new DictionaryConverter<TeamId, int>());
    }
}

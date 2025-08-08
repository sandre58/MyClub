// -----------------------------------------------------------------------
// <copyright file="MatchdayConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for Matchday entities, establishing comprehensive
/// mapping for fixture scheduling data including date management, postponement handling,
/// and complex match organization relationships for league and tournament scheduling.
/// </summary>
/// <remarks>
/// The MatchdayConfiguration provides complete persistence mapping for matchdays, which
/// organize collections of matches into structured rounds or game weeks. The configuration
/// handles sophisticated scheduling scenarios including postponements, date modifications,
/// and complex match assignments for comprehensive fixture management systems.
/// </remarks>
internal sealed class MatchdayConfiguration : IEntityTypeConfiguration<Matchday>
{
    /// <summary>
    /// Configures the Matchday entity with complete mapping for scheduling data including
    /// date handling, postponement management, and match relationship definitions.
    /// </summary>
    /// <param name="builder">The entity type builder for Matchday configuration.</param>
    public void Configure(EntityTypeBuilder<Matchday> builder)
    {
        builder.ConfigureEntity<Matchday, MatchdayId>();

        builder.Property(static x => x.OriginDate).IsRequired();
        builder.Property(nameof(Match.PostponedDate).ToPrivateFieldName()).HasColumnName(nameof(Match.PostponedDate));
        builder.Property(static x => x.IsPostponed).IsRequired();
        builder.OwnsDisplayName(static x => x.DisplayName);
        builder.OwnsAuditableProperties();

        builder.HasManyMatches<MatchdayMatch, Matchday, MatchdayId>();
    }
}

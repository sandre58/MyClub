// -----------------------------------------------------------------------
// <copyright file="RoundConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Converters;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for Round entities, establishing comprehensive
/// mapping for tournament round data including format configuration, hierarchical
/// relationships, and complex associations with fixtures, stages, and teams.
/// </summary>
/// <remarks>
/// The RoundConfiguration provides complete persistence mapping for tournament rounds,
/// which represent elimination phases in knockout competitions. The configuration handles
/// complex round formats, ancestral relationships for tournament progression, and
/// sophisticated team assignment patterns for comprehensive round management.
/// </remarks>
internal sealed class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    /// <summary>
    /// Configures the Round entity with complete mapping for round data including
    /// format conversion, hierarchical relationships, and team management capabilities.
    /// </summary>
    /// <param name="builder">The entity type builder for Round configuration.</param>
    public void Configure(EntityTypeBuilder<Round> builder)
    {
        builder.ConfigureEntity<Round, RoundId>();

        builder.OwnsDisplayName(static x => x.DisplayName);
        builder.OwnsMatchRules(static x => x.Rules);
        builder.Property(static x => x.IsConsolation).IsRequired();
        builder.Property(static x => x.Format).HasConversion(new RoundFormatConverter());
        builder.OwnsAuditableProperties();
        builder.HasOne<Round>().WithMany().HasForeignKey(static x => x.AncestorRoundId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(static x => x.Fixtures).WithOne().OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(static x => x.Stages).WithOne().OnDelete(DeleteBehavior.Cascade);
        builder.HasManyTeams<Round, RoundId, RoundTeam>();
    }
}

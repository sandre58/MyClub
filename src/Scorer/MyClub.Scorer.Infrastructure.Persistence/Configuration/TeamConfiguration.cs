// -----------------------------------------------------------------------
// <copyright file="TeamConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.CompetitionAggregate.Stadiums;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Infrastructure.Persistence.Converters;
using MyClub.Shared.Infrastructure.Persistence.Extensions;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for Team entities, establishing comprehensive
/// mapping for football team data including display properties, colors, geographic
/// information, and relationships with players, staff, and stadiums.
/// </summary>
/// <remarks>
/// The TeamConfiguration provides complete persistence mapping for football teams,
/// handling display information, visual identity through colors and logos, geographic
/// location data, and complex relationships with associated entities including players,
/// staff members, and stadium assignments for comprehensive team management.
/// </remarks>
public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    /// <summary>
    /// Configures the Team entity with complete property mapping, value object ownership,
    /// and relationship definitions for comprehensive football team data persistence.
    /// </summary>
    /// <param name="builder">The entity type builder for Team configuration.</param>
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ConfigureEntity<Team, TeamId>();

        builder.OwnsDisplayName(static x => x.DisplayName);
        builder.Property(static x => x.Logo);
        builder.Property(static x => x.Country).HasConversion(new NullableEnumClassConverter<Country>());
        builder.Property(static x => x.HomeColor);
        builder.Property(static x => x.AwayColor);
        builder.HasOne<Stadium>().WithMany().HasForeignKey(static x => x.StadiumId).OnDelete(DeleteBehavior.SetNull);
        builder.OwnsAuditableProperties();

        builder.HasMany(static x => x.Players).WithOne().HasForeignKey($"{nameof(Team)}Id").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(static x => x.Staff).WithOne().HasForeignKey($"{nameof(Team)}Id").OnDelete(DeleteBehavior.Cascade);
    }
}

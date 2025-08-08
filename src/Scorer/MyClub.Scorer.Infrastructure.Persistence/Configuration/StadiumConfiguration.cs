// -----------------------------------------------------------------------
// <copyright file="StadiumConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.CompetitionAggregate.Stadiums;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for Stadium entities, establishing comprehensive
/// mapping for venue data including geographic information, ground surface details,
/// and address management for complete football stadium administration.
/// </summary>
/// <remarks>
/// The StadiumConfiguration provides complete persistence mapping for football stadiums,
/// handling display information, geographic location data through address value objects,
/// ground surface specifications, and venue management requirements for comprehensive
/// stadium administration and match venue coordination.
/// </remarks>
public class StadiumConfiguration : IEntityTypeConfiguration<Stadium>
{
    /// <summary>
    /// Configures the Stadium entity with complete property mapping, value object ownership,
    /// and ground surface conversion for comprehensive venue data persistence.
    /// </summary>
    /// <param name="builder">The entity type builder for Stadium configuration.</param>
    public void Configure(EntityTypeBuilder<Stadium> builder)
    {
        builder.ConfigureEntity<Stadium, StadiumId>("Stadiums");

        builder.OwnsDisplayName(static x => x.DisplayName, "Stadiums");
        builder.OwnsAddress(static x => x.Address, "Stadiums");
        builder.Property(static x => x.Ground).HasConversion<string>().IsRequired();
        builder.OwnsAuditableProperties();
    }
}

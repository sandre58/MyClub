// -----------------------------------------------------------------------
// <copyright file="PlayerConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Shared.Infrastructure.Persistence.Converters;
using MyClub.Shared.Infrastructure.Persistence.Extensions;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for Player entities, establishing comprehensive
/// mapping for player data including personal information, geographic details, licensing,
/// and contact information for complete football player management.
/// </summary>
/// <remarks>
/// The PlayerConfiguration provides complete persistence mapping for football players,
/// handling personal identification, geographic nationality information, gender tracking,
/// licensing data, and contact details for comprehensive player administration and
/// regulatory compliance in football management systems.
/// </remarks>
public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    /// <summary>
    /// Configures the Player entity with complete property mapping including personal
    /// information, geographic data conversion, and regulatory compliance fields.
    /// </summary>
    /// <param name="builder">The entity type builder for Player configuration.</param>
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ConfigureEntity<Player, PlayerId>();

        builder.Property(static x => x.LastName).IsRequired();
        builder.Property(static x => x.FirstName).IsRequired();
        builder.Property(static x => x.Country).HasConversion(new NullableEnumClassConverter<Country>());
        builder.Property(static x => x.Gender).HasConversion<string>().IsRequired();
        builder.Property(static x => x.Email);
        builder.Property(static x => x.LicenseNumber);
        builder.Property(static x => x.Photo);
        builder.OwnsAuditableProperties();
    }
}

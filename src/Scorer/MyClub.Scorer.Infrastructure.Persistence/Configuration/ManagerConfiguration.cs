// -----------------------------------------------------------------------
// <copyright file="ManagerConfiguration.cs" company="Stéphane ANDRE">
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
/// Entity Framework Core configuration for Manager entities, establishing comprehensive
/// mapping for manager data including personal information, geographic details, licensing,
/// and contact information for complete football team management personnel administration.
/// </summary>
/// <remarks>
/// The ManagerConfiguration provides complete persistence mapping for football managers
/// and coaching staff, handling personal identification, geographic nationality information,
/// gender tracking, licensing data, and contact details for comprehensive team management
/// personnel administration and regulatory compliance in football management systems.
/// </remarks>
public class ManagerConfiguration : IEntityTypeConfiguration<Manager>
{
    /// <summary>
    /// Configures the Manager entity with complete property mapping including personal
    /// information, geographic data conversion, and regulatory compliance fields.
    /// </summary>
    /// <param name="builder">The entity type builder for Manager configuration.</param>
    /// <remarks>
    /// This configuration establishes required last name and first name fields for manager
    /// identification, country property with nullable enum class conversion for nationality
    /// tracking, gender property with string conversion for demographic compliance, email
    /// field for communication, license number field for coaching certification, photo field
    /// for identification, and auditable properties for comprehensive lifecycle tracking.
    /// </remarks>
    public void Configure(EntityTypeBuilder<Manager> builder)
    {
        builder.ConfigureEntity<Manager, ManagerId>();

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

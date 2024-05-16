// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.DatabaseContext.Domain.PlayerAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Configuration
{
    internal class PlayerEntityTypeConfiguration : IEntityTypeConfiguration<Player>
    {
        public void Configure(EntityTypeBuilder<Player> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FirstName).IsRequired();
            builder.Property(x => x.LastName).IsRequired();
            builder.Property(x => x.Street).IsRequired(false);
            builder.Property(x => x.PostalCode).IsRequired(false);
            builder.Property(x => x.City).IsRequired(false);
            builder.Property(x => x.Longitude).IsRequired(false);
            builder.Property(x => x.Latitude).IsRequired(false);
            builder.Property(x => x.AddressCountry).IsRequired(false);
            builder.Property(x => x.Country).IsRequired(false);
            builder.Property(x => x.Category).IsRequired(false).HasDefaultValue("Adult");
            builder.Property(x => x.LicenseNumber).IsRequired(false);
            builder.Property(x => x.Birthdate).IsRequired(false);
            builder.Property(x => x.Description).IsRequired(false);
            builder.Property(x => x.Gender).IsRequired(false).HasDefaultValue("Male");
            builder.Property(x => x.Height).IsRequired(false);
            builder.Property(x => x.Laterality).IsRequired(false).HasDefaultValue("RightHander");
            builder.Property(x => x.Photo).IsRequired(false);
            builder.Property(x => x.PlaceOfBirth).IsRequired(false);
            builder.Property(x => x.Weight).IsRequired(false);
            builder.Property(x => x.Size).IsRequired(false);
            builder.HasMany(x => x.Emails).WithOne(x => x.Player);
            builder.HasMany(x => x.Phones).WithOne(x => x.Player);
            builder.HasMany(x => x.Positions).WithOne(x => x.Player);
            builder.HasMany(x => x.Injuries).WithOne(x => x.Player);
        }
    }
}

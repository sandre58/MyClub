// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.DatabaseContext.Domain.StadiumAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Configuration
{
    internal class StadiumEntityTypeConfiguration : IEntityTypeConfiguration<Stadium>
    {
        public void Configure(EntityTypeBuilder<Stadium> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired();
            builder.Property(x => x.Ground).IsRequired();
            builder.Property(x => x.Street).IsRequired(false);
            builder.Property(x => x.PostalCode).IsRequired(false);
            builder.Property(x => x.City).IsRequired(false);
            builder.Property(x => x.Longitude).IsRequired(false);
            builder.Property(x => x.Latitude).IsRequired(false);
            builder.Property(x => x.Country).IsRequired(false);
        }
    }
}

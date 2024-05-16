// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.DatabaseContext.Domain.ClubAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Configuration
{
    internal class ClubEntityTypeConfiguration : IEntityTypeConfiguration<Club>
    {
        public void Configure(EntityTypeBuilder<Club> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired();
            builder.Property(x => x.Country).IsRequired(false);
            builder.Property(x => x.IsNational).IsRequired();
            builder.Property(x => x.Logo).IsRequired(false);
            builder.Property(x => x.HomeColor).IsRequired(false);
            builder.Property(x => x.AwayColor).IsRequired(false);
            builder.Property(x => x.StadiumId).IsRequired(false);
            builder.Property(x => x.Description).IsRequired(false);
            builder.HasMany(e => e.Teams).WithOne(e => e.Club).HasForeignKey(e => e.ClubId).IsRequired();
        }
    }
}

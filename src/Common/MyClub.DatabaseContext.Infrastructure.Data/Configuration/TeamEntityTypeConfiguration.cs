// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.DatabaseContext.Domain.ClubAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Configuration
{
    internal class TeamEntityTypeConfiguration : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ShortName).IsRequired();
            builder.Property(x => x.Name).IsRequired();
            builder.Property(x => x.Category).IsRequired().HasDefaultValue("Adult");
            builder.Property(x => x.HomeColor).IsRequired(false);
            builder.Property(x => x.AwayColor).IsRequired(false);
            builder.Property(x => x.StadiumId).IsRequired(false);
            builder.HasOne(e => e.Club).WithMany(e => e.Teams).HasForeignKey(e => e.ClubId).IsRequired();
        }
    }
}

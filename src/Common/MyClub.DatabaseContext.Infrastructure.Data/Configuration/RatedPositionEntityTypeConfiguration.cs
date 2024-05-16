// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.DatabaseContext.Domain.PlayerAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Configuration
{
    internal class RatedPositionEntityTypeConfiguration : IEntityTypeConfiguration<RatedPosition>
    {
        public void Configure(EntityTypeBuilder<RatedPosition> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.IsNatural).HasDefaultValue(true);
            builder.Property(x => x.Position).IsRequired().HasDefaultValue("CenterMidfielder");
            builder.Property(x => x.Rating).IsRequired().HasDefaultValue("Natural");
            builder.HasOne(x => x.Player).WithMany(x => x.Positions);
        }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.DatabaseContext.Domain.PlayerAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Configuration
{
    internal class InjuryEntityTypeConfiguration : IEntityTypeConfiguration<Injury>
    {
        public void Configure(EntityTypeBuilder<Injury> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Condition).IsRequired().HasDefaultValue("Injury");
            builder.Property(x => x.Severity).IsRequired().HasDefaultValue("Minor");
            builder.Property(x => x.Type).IsRequired().HasDefaultValue("LeftThigh");
            builder.Property(x => x.Category).IsRequired().HasDefaultValue("Muscular");
            builder.Property(x => x.StartDate).IsRequired();
            builder.Property(x => x.Description).IsRequired(false);
            builder.Property(x => x.EndDate).IsRequired(false);
            builder.HasOne(x => x.Player).WithMany(x => x.Injuries);
        }
    }
}

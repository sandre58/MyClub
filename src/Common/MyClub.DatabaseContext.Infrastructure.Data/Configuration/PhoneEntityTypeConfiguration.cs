// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.DatabaseContext.Domain.PlayerAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Configuration
{
    internal class PhoneEntityTypeConfiguration : IEntityTypeConfiguration<Phone>
    {
        public void Configure(EntityTypeBuilder<Phone> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Value).IsRequired();
            builder.Property(x => x.Label).IsRequired(false);
            builder.Property(x => x.Default).HasDefaultValue(false);
            builder.HasOne(x => x.Player).WithMany(x => x.Phones);
        }
    }
}

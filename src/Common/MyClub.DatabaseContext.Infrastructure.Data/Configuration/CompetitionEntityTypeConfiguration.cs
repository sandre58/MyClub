// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.DatabaseContext.Domain.CompetitionAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Configuration
{
    internal class CompetitionEntityTypeConfiguration : IEntityTypeConfiguration<Competition>
    {
        public void Configure(EntityTypeBuilder<Competition> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired();
            builder.Property(x => x.ShortName).IsRequired();
            builder.Property(x => x.Country).IsRequired(false);
            builder.Property(x => x.Category).IsRequired().HasDefaultValue("Adult");
            builder.Property(x => x.IsNational).IsRequired();
            builder.Property(x => x.Logo).IsRequired(false);
            builder.Property(x => x.Type).IsRequired().HasDefaultValue(Competition.League);
            builder.Property(x => x.StartDate).IsRequired(false);
            builder.Property(x => x.EndDate).IsRequired(false);
            builder.Property(x => x.Description).IsRequired(false);
            builder.Property(x => x.MatchTime).IsRequired(false);
            builder.Property(x => x.RegulationTimeDuration).IsRequired().HasDefaultValue(TimeSpan.FromMinutes(45));
            builder.Property(x => x.RegulationTimeNumber).IsRequired().HasDefaultValue(2);
            builder.Property(x => x.ExtraTimeNumber).IsRequired(false);
            builder.Property(x => x.ExtraTimeDuration).IsRequired(false);
            builder.Property(x => x.NumberOfPenaltyShootouts).IsRequired(false);
            builder.Property(x => x.PointsByGamesWon).IsRequired(false);
            builder.Property(x => x.PointsByGamesDrawn).IsRequired(false);
            builder.Property(x => x.PointsByGamesLost).IsRequired(false);
            builder.Property(x => x.SortingColumns).IsRequired(false);
            builder.Property(x => x.RankLabels).IsRequired(false);
        }
    }
}

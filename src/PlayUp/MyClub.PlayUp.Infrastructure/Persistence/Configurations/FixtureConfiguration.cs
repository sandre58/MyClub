// -----------------------------------------------------------------------
// <copyright file="FixtureConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;

namespace MyClub.PlayUp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for fixtures (Round xor Matchday parent via shadow FKs).
/// </summary>
internal sealed class FixtureConfiguration : IEntityTypeConfiguration<Fixture>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Fixture> builder)
    {
        builder.ToTable(
            "fixtures",
            table => table.HasCheckConstraint(
                "CK_fixtures_round_xor_matchday",
                "(\"round_id\" IS NOT NULL AND \"matchday_id\" IS NULL) OR (\"round_id\" IS NULL AND \"matchday_id\" IS NOT NULL)"));

        builder.HasKey(fixture => fixture.Id);

        builder.Property(fixture => fixture.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<FixtureId>());

        builder.Property(fixture => fixture.SlotAKey)
            .HasColumnName("slot_a_key")
            .HasMaxLength(Slot.SlotKeyMaxLength)
            .IsRequired(false)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(fixture => fixture.SlotBKey)
            .HasColumnName("slot_b_key")
            .HasMaxLength(Slot.SlotKeyMaxLength)
            .IsRequired(false)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(fixture => fixture.BracketPairKey)
            .HasColumnName("bracket_pair_key")
            .HasMaxLength(BracketPair.PairKeyMaxLength)
            .IsRequired(false)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property<RoundId?>("round_id")
            .HasColumnName("round_id")
            .HasColumnType("uuid")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new RoundId(value.Value) : null);

        builder.Property<MatchdayId?>("matchday_id")
            .HasColumnName("matchday_id")
            .HasColumnType("uuid")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new MatchdayId(value.Value) : null);

        builder.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Ignore(fixture => fixture.MatchIds);
        builder.Ignore(fixture => fixture.Attachments);
        builder.Metadata.AddIgnored("_attachments");
    }
}

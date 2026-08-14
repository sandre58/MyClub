// -----------------------------------------------------------------------
// <copyright file="StageConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;

namespace MyClub.PlayUp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the Stage aggregate structure (Draws/Penalties/MatchPlacements ignored).
/// </summary>
internal sealed class StageConfiguration : IEntityTypeConfiguration<Stage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Stage> builder)
    {
        builder.ToTable("stages");
        builder.HasKey(stage => stage.Id);

        builder.Property(stage => stage.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<StageId>());

        builder.Property(stage => stage.CompetitionId)
            .HasColumnName("competition_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<CompetitionId>());

        builder.Property(stage => stage.Name)
            .HasColumnName("name")
            .HasMaxLength(StageName.MaxLength)
            .IsRequired()
            .HasConversion(name => name.Value, value => new StageName(value))
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(stage => stage.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(stage => stage.Regulation)
            .HasColumnName("stage_regulation")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(new StageRegulationJsonConverter(), StageRegulationJsonConverter.Comparer)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Ignore(stage => stage.DomainEvents);
        builder.Ignore(stage => stage.Draws);
        builder.Ignore(stage => stage.Penalties);
        builder.Ignore(stage => stage.MatchPlacements);
        builder.Metadata.AddIgnored("_domainEvents");
        builder.Metadata.AddIgnored("_draws");
        builder.Metadata.AddIgnored("_penalties");
        builder.Metadata.AddIgnored("_matchPlacements");

        builder.HasOne<Competition>()
            .WithMany()
            .HasForeignKey(stage => stage.CompetitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(stage => stage.Groups)
            .WithOne()
            .HasForeignKey("stage_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(stage => stage.Groups)
            .HasField("_groups")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(stage => stage.Rounds)
            .WithOne()
            .HasForeignKey("stage_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(stage => stage.Rounds)
            .HasField("_rounds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(stage => stage.Matchdays)
            .WithOne()
            .HasForeignKey("stage_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(stage => stage.Matchdays)
            .HasField("_matchdays")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(stage => stage.Slots)
            .WithOne()
            .HasForeignKey("stage_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(stage => stage.Slots)
            .HasField("_slots")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(stage => stage.DirectAssignments, ConfigureDirectAssignments);
        builder.Navigation(stage => stage.DirectAssignments)
            .HasField("_directAssignments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureDirectAssignments(OwnedNavigationBuilder<Stage, DirectAssignment> assignments)
    {
        assignments.ToTable("stage_direct_assignments");
        assignments.WithOwner().HasForeignKey("stage_id");
        assignments.HasKey("stage_id", "SlotKey");

        assignments.Property(assignment => assignment.SlotKey)
            .HasColumnName("slot_key")
            .HasMaxLength(Slot.SlotKeyMaxLength)
            .IsRequired();

        assignments.Property(assignment => assignment.EntryId)
            .HasColumnName("entry_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<EntryId>());
    }
}

/// <summary>
/// EF Core mapping for ordered group entry rows.
/// </summary>
internal sealed class GroupEntryRefConfiguration : IEntityTypeConfiguration<GroupEntryRef>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GroupEntryRef> builder)
    {
        builder.ToTable("group_entries");
        builder.HasKey(row => new { row.GroupId, row.EntryId });

        builder.Property(row => row.GroupId)
            .HasColumnName("group_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<GroupId>());

        builder.Property(row => row.EntryId)
            .HasColumnName("entry_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<EntryId>());

        builder.Property(row => row.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(row => row.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core mapping for Stage matchdays.
/// </summary>
internal sealed class MatchdayConfiguration : IEntityTypeConfiguration<Matchday>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Matchday> builder)
    {
        builder.ToTable("matchdays");
        builder.HasKey(matchday => matchday.Id);

        builder.Property(matchday => matchday.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<MatchdayId>());

        builder.Property(matchday => matchday.Number)
            .HasColumnName("number")
            .IsRequired();

        builder.Property<StageId>("stage_id")
            .HasColumnName("stage_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<StageId>());

        builder.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasMany(matchday => matchday.Fixtures)
            .WithOne()
            .HasForeignKey("matchday_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(matchday => matchday.Fixtures)
            .HasField("_fixtures")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

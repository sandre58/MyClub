// -----------------------------------------------------------------------
// <copyright file="StageConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;

namespace MyClub.PlayUp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the Stage aggregate structure including runtime Draws/Penalties/MatchPlacements.
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

        builder.Property(stage => stage.MatchGenerationFormat)
            .HasColumnName("match_generation_format")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(stage => stage.SwissSettings)
            .HasColumnName("swiss_round_count")
            .HasConversion(
                settings => settings == null ? (int?)null : settings.RoundCount,
                value => value == null ? null : new SwissSettings(value.Value))
            .IsRequired(false)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(stage => stage.PlacesPerGroup)
            .HasColumnName("places_per_group")
            .IsRequired(false)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(stage => stage.Regulation)
            .HasColumnName("stage_regulation")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(new StageRegulationJsonConverter(), StageRegulationJsonConverter.Comparer)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(stage => stage.DefaultsBinding)
            .HasColumnName("defaults_binding")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(new DefaultsBindingJsonConverter(), DefaultsBindingJsonConverter.Comparer)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Ignore(stage => stage.DomainEvents);
        builder.Metadata.AddIgnored("_domainEvents");

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

        builder.HasMany(stage => stage.Draws)
            .WithOne()
            .HasForeignKey("stage_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(stage => stage.Draws)
            .HasField("_draws")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(stage => stage.Penalties)
            .WithOne()
            .HasForeignKey("stage_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(stage => stage.Penalties)
            .HasField("_penalties")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(stage => stage.DirectAssignments, ConfigureDirectAssignments);
        builder.Navigation(stage => stage.DirectAssignments)
            .HasField("_directAssignments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(stage => stage.AffectationAuthoring, ConfigureAffectationAuthoring);
        builder.Navigation(stage => stage.AffectationAuthoring)
            .HasField("_affectationAuthoring")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(stage => stage.CompositionEntries, ConfigureCompositionEntries);
        builder.Navigation(stage => stage.CompositionEntries)
            .HasField("_compositionEntries")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(stage => stage.FormPathResolutions, ConfigureFormPathResolutions);
        builder.Navigation(stage => stage.FormPathResolutions)
            .HasField("_formPathResolutions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(stage => stage.GroupResolutionProvenances, ConfigureGroupResolutionProvenances);
        builder.Navigation(stage => stage.GroupResolutionProvenances)
            .HasField("_groupResolutionProvenances")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(stage => stage.MatchPlacements, ConfigureMatchPlacements);
        builder.Navigation(stage => stage.MatchPlacements)
            .HasField("_matchPlacements")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(stage => stage.SwissByeHistory, ConfigureSwissByeHistory);
        builder.Navigation(stage => stage.SwissByeHistory)
            .HasField("_swissByeHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(stage => stage.BracketPairs, ConfigureBracketPairs);
        builder.Navigation(stage => stage.BracketPairs)
            .HasField("_bracketPairs")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureBracketPairs(OwnedNavigationBuilder<Stage, BracketPair> pairs)
    {
        pairs.ToTable("stage_bracket_pairs");
        pairs.WithOwner().HasForeignKey("stage_id");
        pairs.HasKey("stage_id", "PairKey");

        pairs.Property(pair => pair.PairKey)
            .HasColumnName("pair_key")
            .HasMaxLength(BracketPair.PairKeyMaxLength)
            .IsRequired();

        pairs.Property(pair => pair.SlotAKey)
            .HasColumnName("slot_a_key")
            .HasMaxLength(Slot.SlotKeyMaxLength)
            .IsRequired();

        pairs.Property(pair => pair.SlotBKey)
            .HasColumnName("slot_b_key")
            .HasMaxLength(Slot.SlotKeyMaxLength)
            .IsRequired();
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

    private static void ConfigureAffectationAuthoring(OwnedNavigationBuilder<Stage, CompositionEntry> entries)
    {
        entries.ToTable("stage_affectation_entries");
        entries.WithOwner().HasForeignKey("stage_id");
        entries.HasKey("stage_id", "EntryId");

        entries.Property(entry => entry.EntryId)
            .HasColumnName("entry_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<EntryId>());

        entries.Property(entry => entry.ResolutionProvenanceKind)
            .HasColumnName("resolution_provenance_kind")
            .HasConversion<int>()
            .IsRequired(false);

        entries.Property(entry => entry.ResolutionSourcePathId)
            .HasColumnName("resolution_source_path_id")
            .HasMaxLength(ResolutionProvenanceConfiguration.SourcePathIdMaxLength)
            .IsRequired(false);

        entries.Property(entry => entry.ResolutionUpstreamFingerprint)
            .HasColumnName("resolution_upstream_fingerprint")
            .HasMaxLength(ResolutionProvenanceConfiguration.UpstreamFingerprintMaxLength)
            .IsRequired(false);
    }

    private static void ConfigureCompositionEntries(OwnedNavigationBuilder<Stage, CompositionEntry> entries)
    {
        entries.ToTable("stage_composition_entries");
        entries.WithOwner().HasForeignKey("stage_id");
        entries.HasKey("stage_id", "EntryId");

        entries.Property(entry => entry.EntryId)
            .HasColumnName("entry_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<EntryId>());

        entries.Property(entry => entry.ResolutionProvenanceKind)
            .HasColumnName("resolution_provenance_kind")
            .HasConversion<int>()
            .IsRequired(false);

        entries.Property(entry => entry.ResolutionSourcePathId)
            .HasColumnName("resolution_source_path_id")
            .HasMaxLength(ResolutionProvenanceConfiguration.SourcePathIdMaxLength)
            .IsRequired(false);

        entries.Property(entry => entry.ResolutionUpstreamFingerprint)
            .HasColumnName("resolution_upstream_fingerprint")
            .HasMaxLength(ResolutionProvenanceConfiguration.UpstreamFingerprintMaxLength)
            .IsRequired(false);
    }

    private static void ConfigureGroupResolutionProvenances(
        OwnedNavigationBuilder<Stage, GroupResolutionProvenance> rows)
    {
        rows.ToTable("stage_group_resolution_provenances");
        rows.WithOwner().HasForeignKey("stage_id");
        rows.HasKey("stage_id", nameof(GroupResolutionProvenance.GroupId), nameof(GroupResolutionProvenance.EntryId));

        rows.Property(row => row.GroupId)
            .HasColumnName("group_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<GroupId>());

        rows.Property(row => row.EntryId)
            .HasColumnName("entry_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<EntryId>());

        rows.Property(row => row.ProvenanceKind)
            .HasColumnName("resolution_provenance_kind")
            .HasConversion<int>();

        rows.Property(row => row.SourcePathId)
            .HasColumnName("resolution_source_path_id")
            .HasMaxLength(ResolutionProvenanceConfiguration.SourcePathIdMaxLength);

        rows.Property(row => row.UpstreamResultFingerprint)
            .HasColumnName("resolution_upstream_fingerprint")
            .HasMaxLength(ResolutionProvenanceConfiguration.UpstreamFingerprintMaxLength);
    }

    private static void ConfigureFormPathResolutions(OwnedNavigationBuilder<Stage, FormPathResolution> resolutions)
    {
        resolutions.ToTable("stage_form_path_resolutions");
        resolutions.WithOwner().HasForeignKey("stage_id");
        resolutions.HasKey("stage_id", "PathFingerprint");

        resolutions.Property(resolution => resolution.PathFingerprint)
            .HasColumnName("path_fingerprint")
            .HasMaxLength(256)
            .IsRequired();

        resolutions.Property(resolution => resolution.EntryId)
            .HasColumnName("entry_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<EntryId>());
    }

    private static void ConfigureMatchPlacements(OwnedNavigationBuilder<Stage, MatchPlacement> placements)
    {
        placements.ToTable("match_placements");
        placements.WithOwner().HasForeignKey("stage_id");
        placements.HasKey("stage_id", "MatchId");

        placements.Property(placement => placement.MatchId)
            .HasColumnName("match_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<MatchId>());

        placements.Property(placement => placement.Start)
            .HasColumnName("start")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        placements.Property(placement => placement.ResourceId)
            .HasColumnName("resource_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<ResourceId>());

        placements.HasOne<Match>()
            .WithMany()
            .HasForeignKey(placement => placement.MatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureSwissByeHistory(OwnedNavigationBuilder<Stage, SwissBye> byes)
    {
        byes.ToTable("stage_swiss_byes");
        byes.WithOwner().HasForeignKey("stage_id");
        byes.HasKey("stage_id", "RoundIndex");

        byes.Property(bye => bye.RoundIndex)
            .HasColumnName("round_index")
            .ValueGeneratedNever()
            .IsRequired();

        byes.Property(bye => bye.EntryId)
            .HasColumnName("entry_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<EntryId>());
    }
}

// -----------------------------------------------------------------------
// <copyright file="MatchConfiguration.cs" company="Stéphane ANDRE">
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
/// EF Core mapping for the Match aggregate.
/// </summary>
internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");
        builder.HasKey(match => match.Id);

        builder.Property(match => match.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<MatchId>());

        builder.Property(match => match.CompetitionId)
            .HasColumnName("competition_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<CompetitionId>());

        builder.Property(match => match.StageId)
            .HasColumnName("stage_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<StageId>());

        builder.Property(match => match.HomeEntryId)
            .HasColumnName("home_entry_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<EntryId>());

        builder.Property(match => match.AwayEntryId)
            .HasColumnName("away_entry_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<EntryId>());

        builder.Property(match => match.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(match => match.Result)
            .HasColumnName("result")
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasConversion(new MatchResultJsonConverter(), MatchResultJsonConverter.Comparer)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(match => match.RunningScore)
            .HasColumnName("running_score")
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasConversion(new RunningScoreJsonConverter(), RunningScoreJsonConverter.Comparer)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Ignore(match => match.DomainEvents);
        builder.Metadata.AddIgnored("_domainEvents");

        builder.OwnsMany(match => match.DeclaredParticipations, ConfigureDeclaredParticipations);
        builder.Navigation(nameof(Match.DeclaredParticipations))
            .HasField("_declaredParticipations")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(match => match.RecordedGoals, ConfigureRecordedGoals);
        builder.Navigation(nameof(Match.RecordedGoals))
            .HasField("_recordedGoals")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(match => match.RecordedSubstitutions, ConfigureRecordedSubstitutions);
        builder.Navigation(nameof(Match.RecordedSubstitutions))
            .HasField("_recordedSubstitutions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(match => match.RecordedDisciplinaryEvents, ConfigureRecordedDisciplinaryEvents);
        builder.Navigation(nameof(Match.RecordedDisciplinaryEvents))
            .HasField("_recordedDisciplinaryEvents")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Competition>()
            .WithMany()
            .HasForeignKey(match => match.CompetitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Stage>()
            .WithMany()
            .HasForeignKey(match => match.StageId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureDeclaredParticipations(OwnedNavigationBuilder<Match, DeclaredParticipation> participations)
    {
        participations.ToTable("match_declared_participations");
        participations.WithOwner().HasForeignKey("match_id");
        participations.HasKey(participation => participation.Id);

        participations.Property(participation => participation.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<MemberId>());

        participations.Property(participation => participation.Side)
            .HasColumnName("side")
            .HasConversion<int>()
            .IsRequired();

        participations.Property(participation => participation.CompositionStatus)
            .HasColumnName("composition_status")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        participations.Property(participation => participation.JerseyNumber)
            .HasColumnName("jersey_number")
            .IsRequired(false)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        participations.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();

        participations.HasIndex("match_id", "SortOrder").IsUnique();
    }

    private static void ConfigureRecordedGoals(OwnedNavigationBuilder<Match, RecordedGoal> goals)
    {
        goals.ToTable("match_recorded_goals");
        goals.WithOwner().HasForeignKey("match_id");
        goals.HasKey(goal => goal.Id);

        goals.Property(goal => goal.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<GoalId>());

        goals.Property(goal => goal.ScorerMemberId)
            .HasColumnName("scorer_member_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<MemberId>())
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        goals.Property(goal => goal.CreditedSide)
            .HasColumnName("credited_side")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        goals.Property(goal => goal.AssisterMemberId)
            .HasColumnName("assister_member_id")
            .HasColumnType("uuid")
            .IsRequired(false)
            .HasConversion(new GuidTypedIdConverter<MemberId>())
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        goals.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();

        goals.HasIndex("match_id", "SortOrder").IsUnique();
    }

    private static void ConfigureRecordedSubstitutions(OwnedNavigationBuilder<Match, RecordedSubstitution> substitutions)
    {
        substitutions.ToTable("match_recorded_substitutions");
        substitutions.WithOwner().HasForeignKey("match_id");
        substitutions.HasKey(substitution => substitution.Id);

        substitutions.Property(substitution => substitution.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<SubstitutionId>());

        substitutions.Property(substitution => substitution.Side)
            .HasColumnName("side")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        substitutions.Property(substitution => substitution.OutMemberId)
            .HasColumnName("out_member_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<MemberId>())
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        substitutions.Property(substitution => substitution.InMemberId)
            .HasColumnName("in_member_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<MemberId>())
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        substitutions.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();

        substitutions.HasIndex("match_id", "SortOrder").IsUnique();
    }

    private static void ConfigureRecordedDisciplinaryEvents(
        OwnedNavigationBuilder<Match, RecordedDisciplinaryEvent> events)
    {
        events.ToTable("match_recorded_disciplinary_events");
        events.WithOwner().HasForeignKey("match_id");
        events.HasKey(evt => evt.Id);

        events.Property(evt => evt.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<DisciplinaryEventId>());

        events.Property(evt => evt.MemberId)
            .HasColumnName("member_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<MemberId>())
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        events.Property(evt => evt.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        events.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();

        events.HasIndex("match_id", "SortOrder").IsUnique();
    }
}

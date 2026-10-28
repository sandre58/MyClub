// -----------------------------------------------------------------------
// <copyright file="InsertCompetitionStageTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

/// <summary>
/// Positioned stage birth: order, invalid anchor, lifecycle, wires, append.
/// </summary>
public sealed class InsertCompetitionStageTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 26, 8, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_inserts_before_the_first_stage()
    {
        var competition = CreateCompetition.Execute("Insert-First", _clock);
        var middle = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Middle"), _clock);
        var last = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Last"), _clock);

        var first = InsertCompetitionStage.Execute(
            competition,
            StructureIntent.Championship("First"),
            middle.Id,
            StageInsertPosition.Before,
            _clock);

        competition.StageIds.Should().Equal(first.Id, middle.Id, last.Id);
    }

    [Fact]
    public void Execute_inserts_between_stages()
    {
        var competition = CreateCompetition.Execute("Insert-Middle", _clock);
        var first = AddCompetitionStage.Execute(competition, StructureIntent.Championship("First"), _clock);
        var last = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Last"), _clock);

        var middle = InsertCompetitionStage.Execute(
            competition,
            StructureIntent.Cup(4, "Middle"),
            last.Id,
            StageInsertPosition.Before,
            _clock);

        competition.StageIds.Should().Equal(first.Id, middle.Id, last.Id);
        middle.Name.Value.Should().Be("Middle");
    }

    [Fact]
    public void Execute_inserts_after_the_last_stage()
    {
        var competition = CreateCompetition.Execute("Insert-Last", _clock);
        var first = AddCompetitionStage.Execute(competition, StructureIntent.Championship("First"), _clock);
        var middle = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Middle"), _clock);

        var last = InsertCompetitionStage.Execute(
            competition,
            StructureIntent.Championship("Last"),
            middle.Id,
            StageInsertPosition.After,
            _clock);

        competition.StageIds.Should().Equal(first.Id, middle.Id, last.Id);
    }

    [Fact]
    public void Execute_rejects_unknown_anchor_without_attaching_a_stage()
    {
        var competition = CreateCompetition.Execute("Insert-Anchor", _clock);
        var only = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Only"), _clock);
        var before = competition.StageIds.ToArray();

        var act = () => InsertCompetitionStage.Execute(
            competition,
            StructureIntent.Championship("Ghost"),
            StageId.New(),
            StageInsertPosition.Before,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
        competition.StageIds.Should().Equal(before);
        competition.StageIds.Should().Equal(only.Id);
    }

    [Fact]
    public void Execute_rejects_undefined_position_without_attaching_a_stage()
    {
        var competition = CreateCompetition.Execute("Insert-Position", _clock);
        var only = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Only"), _clock);

        var act = () => InsertCompetitionStage.Execute(
            competition,
            StructureIntent.Championship("Ghost"),
            only.Id,
            (StageInsertPosition)9,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
        competition.StageIds.Should().Equal(only.Id);
    }

    [Theory]
    [InlineData(CompetitionStatus.Running)]
    [InlineData(CompetitionStatus.Suspended)]
    [InlineData(CompetitionStatus.Completed)]
    [InlineData(CompetitionStatus.Archived)]
    public void Execute_refuses_when_competition_lifecycle_is_closed(CompetitionStatus status)
    {
        var competition = CreateCompetition.Execute($"Insert-{status}", _clock);
        var only = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "Only"), _clock);
        AddEntry.Execute(competition, "Alpha", _clock);
        Reach(competition, status);
        var before = competition.StageIds.ToArray();

        var act = () => InsertCompetitionStage.Execute(
            competition,
            StructureIntent.Cup(2, "TooLate"),
            only.Id,
            StageInsertPosition.After,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
        competition.StageIds.Should().Equal(before);
        competition.Status.Should().Be(status);
    }

    [Fact]
    public void Execute_on_Ready_places_the_stage_and_demotes_to_Draft()
    {
        var competition = CreateCompetition.Execute("Insert-Ready", _clock);
        var anchor = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Anchor"), _clock);
        AddEntry.Execute(competition, "Alpha", _clock);
        competition.Prepare(_clock);

        var inserted = InsertCompetitionStage.Execute(
            competition,
            StructureIntent.Championship("Before"),
            anchor.Id,
            StageInsertPosition.Before,
            _clock);

        competition.Status.Should().Be(CompetitionStatus.Draft);
        competition.StageIds.Should().Equal(inserted.Id, anchor.Id);
    }

    [Fact]
    public void Execute_allows_insert_when_a_stage_is_Ready_and_the_competition_is_Draft()
    {
        var competition = CreateCompetition.Execute("Insert-StageReady", _clock);
        var ready = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Ready"), _clock);
        ready.AddMatchday(1, _clock);
        ready.Prepare(_clock);
        var draft = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Draft"), _clock);

        var inserted = InsertCompetitionStage.Execute(
            competition,
            StructureIntent.Championship("Between"),
            draft.Id,
            StageInsertPosition.Before,
            _clock);

        competition.Status.Should().Be(CompetitionStatus.Draft);
        ready.Status.Should().Be(StageStatus.Ready);
        competition.StageIds.Should().Equal(ready.Id, inserted.Id, draft.Id);
    }

    [Fact]
    public void Execute_does_not_rewrite_qualification_or_progression_wires()
    {
        var competition = CreateCompetition.Execute("Insert-Wires", _clock);
        var source = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "Source"), _clock);
        var pair = source.BracketPairs.Should().ContainSingle().Subject;
        source.AddFixture(source.Rounds[0].Id, _clock, pair.SlotAKey, pair.SlotBKey, pair.PairKey);
        var destination = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "Destination"), _clock);
        AddStageSlot.Execute(destination, competition, "QF1");

        ReplaceStageQualificationRules.Execute(
            source,
            [
                new QualificationPathSpec(
                    Order: 1,
                    SelectionMode.Position,
                    SelectionValue: 1,
                    destination.Id.Value,
                    RankingScope.Overall)
            ],
            _clock);
        ReplaceStageProgressionRules.Execute(
            source,
            [
                new ProgressionPathSpec(
                    pair.PairKey,
                    ProgressionOutcome.Winner,
                    destination.Id,
                    DestinationSlotKey: "QF1")
            ],
            _clock);

        var qualification = source.Regulation.QualificationRules;
        var progression = source.Regulation.ProgressionRules;

        InsertCompetitionStage.Execute(
            competition,
            StructureIntent.Championship("Between"),
            destination.Id,
            StageInsertPosition.Before,
            _clock);

        source.Regulation.QualificationRules.Should().BeSameAs(qualification);
        source.Regulation.ProgressionRules.Should().BeSameAs(progression);
        qualification!.Paths.Should().ContainSingle()
            .Which.Destination.StageId.Should().Be(destination.Id);
        progression!.Paths.Should().ContainSingle()
            .Which.Destination.StageId.Should().Be(destination.Id);
    }

    [Fact]
    public void AddCompetitionStage_still_appends_after_a_positioned_insert()
    {
        var competition = CreateCompetition.Execute("Append", _clock);
        var first = AddCompetitionStage.Execute(competition, StructureIntent.Championship("First"), _clock);
        var last = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Last"), _clock);
        var middle = InsertCompetitionStage.Execute(
            competition,
            StructureIntent.Championship("Middle"),
            last.Id,
            StageInsertPosition.Before,
            _clock);

        var appended = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Appended"), _clock);

        competition.StageIds.Should().Equal(first.Id, middle.Id, last.Id, appended.Id);
    }

    private void Reach(Competition competition, CompetitionStatus status)
    {
        competition.Prepare(_clock);
        if (status == CompetitionStatus.Ready)
        {
            return;
        }

        competition.Start(_clock);
        switch (status)
        {
            case CompetitionStatus.Running:
                return;
            case CompetitionStatus.Suspended:
                competition.Suspend(_clock);
                return;
        }

        competition.Complete(CompletionMode.Normal, _clock);
        if (status == CompetitionStatus.Completed)
        {
            return;
        }

        competition.Archive(_clock);
    }
}

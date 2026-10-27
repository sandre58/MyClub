// -----------------------------------------------------------------------
// <copyright file="SetCompetitionStageOrderTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

/// <summary>
/// Stage reorder is a permutation of StageIds and leaves Qualif/Prog wires untouched.
/// </summary>
public sealed class SetCompetitionStageOrderTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 26, 8, 30, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_permutes_stage_ids()
    {
        var competition = CreateCompetition.Execute("Order", _clock);
        var first = AddCompetitionStage.Execute(competition, StructureIntent.Championship("First"), _clock);
        var second = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Second"), _clock);
        var third = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Third"), _clock);

        SetCompetitionStageOrder.Execute(competition, [third.Id, first.Id, second.Id], _clock);

        competition.StageIds.Should().Equal(third.Id, first.Id, second.Id);
    }

    [Fact]
    public void Execute_on_Ready_demotes_when_the_order_changes()
    {
        var competition = CreateCompetition.Execute("Order-Ready", _clock);
        var first = AddCompetitionStage.Execute(competition, StructureIntent.Championship("First"), _clock);
        var second = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Second"), _clock);
        AddEntry.Execute(competition, "Alpha", _clock);
        competition.Prepare(_clock);

        SetCompetitionStageOrder.Execute(competition, [second.Id, first.Id], _clock);

        competition.Status.Should().Be(CompetitionStatus.Draft);
        competition.StageIds.Should().Equal(second.Id, first.Id);
    }

    [Fact]
    public void Execute_allows_reorder_when_a_stage_is_Ready_and_the_competition_is_Draft()
    {
        var competition = CreateCompetition.Execute("Order-StageReady", _clock);
        var ready = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Ready"), _clock);
        ready.AddMatchday(1, _clock);
        ready.Prepare(_clock);
        var draft = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Draft"), _clock);

        SetCompetitionStageOrder.Execute(competition, [draft.Id, ready.Id], _clock);

        competition.Status.Should().Be(CompetitionStatus.Draft);
        ready.Status.Should().Be(StageStatus.Ready);
        competition.StageIds.Should().Equal(draft.Id, ready.Id);
    }

    [Fact]
    public void Execute_same_order_on_Ready_is_a_noop()
    {
        var competition = CreateCompetition.Execute("Order-Noop", _clock);
        var only = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Only"), _clock);
        AddEntry.Execute(competition, "Alpha", _clock);
        competition.Prepare(_clock);

        SetCompetitionStageOrder.Execute(competition, [only.Id], _clock);

        competition.Status.Should().Be(CompetitionStatus.Ready);
        competition.StageIds.Should().Equal(only.Id);
    }

    [Fact]
    public void Execute_rejects_a_non_permutation_without_changing_order()
    {
        var competition = CreateCompetition.Execute("Order-Bad", _clock);
        var only = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Only"), _clock);

        var act = () => SetCompetitionStageOrder.Execute(
            competition,
            [only.Id, StageId.New()],
            _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(CompetitionErrorCodes.InvalidStageOrder);
        competition.StageIds.Should().Equal(only.Id);
    }

    [Theory]
    [InlineData(CompetitionStatus.Running)]
    [InlineData(CompetitionStatus.Suspended)]
    [InlineData(CompetitionStatus.Completed)]
    [InlineData(CompetitionStatus.Archived)]
    public void Execute_refuses_when_competition_lifecycle_is_closed(CompetitionStatus status)
    {
        var competition = CreateCompetition.Execute($"Order-{status}", _clock);
        var first = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "First"), _clock);
        var second = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "Second"), _clock);
        AddEntry.Execute(competition, "Alpha", _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        if (status == CompetitionStatus.Suspended)
        {
            competition.Suspend(_clock);
        }
        else if (status is CompetitionStatus.Completed or CompetitionStatus.Archived)
        {
            competition.Complete(CompletionMode.Normal, _clock);
            if (status == CompetitionStatus.Archived)
            {
                competition.Archive(_clock);
            }
        }

        var act = () => SetCompetitionStageOrder.Execute(competition, [second.Id, first.Id], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
        competition.StageIds.Should().Equal(first.Id, second.Id);
        competition.Status.Should().Be(status);
    }

    [Fact]
    public void Execute_does_not_rewrite_qualification_or_progression_wires()
    {
        var competition = CreateCompetition.Execute("Order-Wires", _clock);
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

        SetCompetitionStageOrder.Execute(competition, [destination.Id, source.Id], _clock);

        competition.StageIds.Should().Equal(destination.Id, source.Id);
        source.Regulation.QualificationRules.Should().BeSameAs(qualification);
        source.Regulation.ProgressionRules.Should().BeSameAs(progression);
        qualification!.Paths[0].Destination.StageId.Should().Be(destination.Id);
        progression!.Paths[0].Destination.StageId.Should().Be(destination.Id);
    }
}

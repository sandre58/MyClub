// -----------------------------------------------------------------------
// <copyright file="StructureStageLifecycleAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureStageLifecycleAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 10, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_Draft_championship_projects_Prepare_enabled_and_checklist()
    {
        var situation = TestCompetition.Create("Lifecycle Draft", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"));
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle.Should().NotBeNull();
        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        hub.Lifecycle.Transition.Enabled.Should().BeTrue();
        hub.Lifecycle.PrepareChecklist.Should().NotBeNull();
        hub.Lifecycle.PrepareChecklist!.Items.Should().Contain(item =>
            item.Code == StructureStageLifecycleAssembler.ChecklistMatches
            && item.Applicable
            && !item.Satisfied);
        hub.Lifecycle.MatchProgress.Should().BeNull();
    }

    [Fact]
    public void Assemble_Ready_projects_Start_enabled()
    {
        var situation = TestCompetition.Create("Lifecycle Ready", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionStartStage);
        hub.Lifecycle.Transition.Enabled.Should().BeTrue();
        hub.Lifecycle.PrepareChecklist.Should().BeNull();
    }

    [Fact]
    public void Assemble_Running_with_open_match_blocks_Complete()
    {
        var (competition, stage, match) = CreateRunningWithMatch(MatchStatus.Scheduled);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = [ToSummaryRow(match)]
        };
        var view = StructureViewAssembler.Assemble(competition, [stage], matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Should().BeEquivalentTo(
            new StructureStageLifecycleTransitionDto(
                OverviewAssembler.ActionCompleteStage,
                false,
                CompleteStage.ReasonOpenMatches));
        hub.Lifecycle.MatchProgress!.Open.Should().Be(1);
        hub.Lifecycle.MatchProgress.CanComplete.Should().BeFalse();
    }

    [Fact]
    public void Assemble_Running_with_finished_match_allows_Complete()
    {
        var (competition, stage, match) = CreateRunningWithMatch(MatchStatus.Finished);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = [ToSummaryRow(match)]
        };
        var view = StructureViewAssembler.Assemble(competition, [stage], matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionCompleteStage);
        hub.Lifecycle.Transition.Enabled.Should().BeTrue();
        hub.Lifecycle.MatchProgress!.CanComplete.Should().BeTrue();
    }

    [Fact]
    public void Assemble_Running_without_matches_allows_Complete()
    {
        var situation = TestCompetition.Create("Lifecycle Empty Running", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage()
            .StartPrimaryStage();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Enabled.Should().BeTrue();
        hub.Lifecycle.MatchProgress!.Total.Should().Be(0);
        hub.Lifecycle.MatchProgress.CanComplete.Should().BeTrue();
    }

    [Fact]
    public void Assemble_competition_Running_freezes_Draft_phase_Prepare()
    {
        var competition = Competition.Create(new CompetitionName("Frozen"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledConstructionFrozen);
    }

    private static MatchSummaryRow ToSummaryRow(Match match) =>
        new(match.Id, match.StageId, match.Status, match.HomeEntryId, match.AwayEntryId, match.Result);

    private (Competition Competition, Stage Stage, Match Match) CreateRunningWithMatch(MatchStatus status)
    {
        var competition = Competition.Create(new CompetitionName("Running"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        PrepareStage.Execute(stage, [stage], _clock);
        StartStage.Execute(stage, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        if (status == MatchStatus.Finished)
        {
            match.Start(_clock);
            match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        }

        return (competition, stage, match);
    }
}

// -----------------------------------------------------------------------
// <copyright file="StartStageReadinessTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class StartStageReadinessTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 12, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Start_blocked_when_championship_has_unmaterialized_eligible_confrontations()
    {
        var situation = TestCompetition.Create("Ch", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();

        var act = () => StageStartReadinessGuard.EnsureReadyToStart(stage, situation.Competition, []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StartBlockedUnmaterialized);
    }

    [Fact]
    public void Start_allowed_when_championship_fully_materialized()
    {
        var situation = TestCompetition.Create("Ch", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        var summaries = StageStartReadinessGuard.ToSummaryRows(materialized.CreatedMatches);

        var act = () => StageStartReadinessGuard.EnsureReadyToStart(stage, situation.Competition, summaries);

        act.Should().NotThrow();
        StartStage.Execute(stage, _clock);
        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void Start_allowed_for_swiss_without_matches()
    {
        var situation = TestCompetition.Create("Swiss", _clock)
            .WithTeams(4)
            .WithStructure(StructureIntent.Swiss(roundCount: 3))
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();

        var act = () => StageStartReadinessGuard.EnsureReadyToStart(stage, situation.Competition, []);

        act.Should().NotThrow();
    }

    [Fact]
    public void Read_Start_disabled_when_unmaterialized_confrontations_remain()
    {
        var situation = TestCompetition.Create("Ch", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionStartStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations);
    }

    [Fact]
    public void Read_Start_enabled_when_championship_fully_materialized()
    {
        var situation = TestCompetition.Create("Ch", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = StageStartReadinessGuard.ToSummaryRows(materialized.CreatedMatches)
        };
        var view = StructureViewAssembler.Assemble(
            situation.Competition,
            situation.Stages,
            matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Enabled.Should().BeTrue();
    }

    [Fact]
    public void TryGetDisabledReasonCode_cup_ready_slotted_non_playable_initial_round()
    {
        var situation = TestCompetition.Create("Cup IRR guard", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        var round = stage.Rounds[0];
        stage.AddFixture(round.Id, _clock, "S1", "S2", "P1");
        stage.AddFixture(round.Id, _clock, "S3", "S4", "P2");
        situation.PreparePrimaryStage();

        var reason = StageStartReadinessGuard.TryGetDisabledReasonCode(
            stage,
            situation.Competition,
            [],
            situation.Stages);

        reason.Should().Be(StructureStageLifecycleAssembler.DisabledInitialRoundNotPlayable);
    }
}

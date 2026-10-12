// -----------------------------------------------------------------------
// <copyright file="RevertStageLifecycleTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class RevertStageLifecycleTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void RevertToDraft_succeeds_when_no_matches()
    {
        var situation = TestCompetition.Create("R", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        stage.Prepare(_clock);

        RevertToDraft.Execute(stage, [], _clock);

        stage.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void RevertToDraft_blocked_when_matches_attached()
    {
        var situation = TestCompetition.Create("R", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        StageMaterializationTestSupport.EnsureReadyForMaterialize(stage, [stage], _clock);
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);

        var act = () => RevertToDraft.Execute(stage, materialized.CreatedMatches, _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.RevertToDraftBlocked);
    }

    [Fact]
    public void SuspendStage_keeps_scheduled_matches()
    {
        var situation = TestCompetition.Create("R", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        StageMaterializationTestSupport.EnsureReadyForMaterialize(stage, [stage], _clock);
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        stage.Start(_clock);

        SuspendStage.Execute(stage, materialized.CreatedMatches, _clock);

        stage.Status.Should().Be(StageStatus.Suspended);
        materialized.CreatedMatches.Should().OnlyContain(m => stage.HasMatch(m.Id));
    }

    [Fact]
    public void SuspendStage_succeeds_when_some_matches_finished_and_others_scheduled()
    {
        var situation = TestCompetition.Create("R", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        StageMaterializationTestSupport.EnsureReadyForMaterialize(stage, [stage], _clock);
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        stage.Start(_clock);
        materialized.CreatedMatches[0].Finish(
            new MatchResult(ResultType.Played, new Score(1, 0)),
            _clock);

        SuspendStage.Execute(stage, materialized.CreatedMatches, _clock);

        stage.Status.Should().Be(StageStatus.Suspended);
    }

    [Fact]
    public void SuspendStage_blocked_when_match_is_live()
    {
        var situation = TestCompetition.Create("R", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        StageMaterializationTestSupport.EnsureReadyForMaterialize(stage, [stage], _clock);
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        stage.Start(_clock);
        materialized.CreatedMatches[0].Start(_clock);

        var act = () => SuspendStage.Execute(stage, materialized.CreatedMatches, _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.RevertToReadyBlocked);
    }

    [Fact]
    public void RevertToReady_blocked_when_match_is_finished()
    {
        var situation = TestCompetition.Create("R", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        StageMaterializationTestSupport.EnsureReadyForMaterialize(stage, [stage], _clock);
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        stage.Start(_clock);
        materialized.CreatedMatches[0].Start(_clock);
        materialized.CreatedMatches[0].Finish(
            new MatchResult(ResultType.Played, new Score(1, 0)),
            _clock);

        var act = () => RevertToReady.Execute(stage, materialized.CreatedMatches, _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.RevertToReadyBlocked);
    }

    [Fact]
    public void RevertToReady_from_Running_sets_Ready_when_matches_scheduled()
    {
        var situation = TestCompetition.Create("R", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        StageMaterializationTestSupport.EnsureReadyForMaterialize(stage, [stage], _clock);
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        stage.Start(_clock);

        RevertToReady.Execute(stage, materialized.CreatedMatches, _clock);

        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void ResumeStage_from_Suspended_sets_Running()
    {
        var situation = TestCompetition.Create("R", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        StageMaterializationTestSupport.EnsureReadyForMaterialize(stage, [stage], _clock);
        MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        stage.Start(_clock);
        stage.Suspend(_clock);

        ResumeStage.Execute(stage, _clock);

        stage.Status.Should().Be(StageStatus.Running);
    }
}

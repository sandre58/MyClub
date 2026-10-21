// -----------------------------------------------------------------------
// <copyright file="RevertToDraftReadAlignmentTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class RevertToDraftReadAlignmentTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 12, 15, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Orphan_match_on_stage_does_not_block_RevertToDraft_command_or_read()
    {
        var situation = TestCompetition.Create("R", _clock)
            .WithTeams("A", "B")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        stage.Prepare(_clock);

        var orphan = Match.Create(
            situation.Competition.Id,
            stage.Id,
            situation.Competition.Entries[0].Id,
            situation.Competition.Entries[1].Id,
            _clock);

        RevertToDraft.Execute(stage, [orphan], _clock);
        stage.Status.Should().Be(StageStatus.Draft);

        stage.Prepare(_clock);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] =
            [
                new MatchSummaryRow(
                    orphan.Id,
                    orphan.StageId,
                    orphan.Status,
                    orphan.HomeEntryId,
                    orphan.AwayEntryId,
                    orphan.Result)
            ]
        };
        var view = StructureViewAssembler.Assemble(
            situation.Competition,
            [stage],
            matchSummariesByStage: summaries);
        var revert = view.Stages.Single().Lifecycle!.RevertTransition;

        revert.Should().NotBeNull();
        revert.Code.Should().Be(OverviewAssembler.ActionRevertToDraftStage);
        revert.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Attached_match_blocks_RevertToDraft_command_and_read()
    {
        var situation = TestCompetition.Create("R", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        StageMaterializationTestSupport.EnsureReadyForMaterialize(
            situation.Competition,
            stage,
            situation.Stages,
            _clock);
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);

        var act = () => RevertToDraft.Execute(stage, materialized.CreatedMatches, _clock);
        act.Should().Throw<ApplicationFailureException>();

        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = StageStartReadinessGuard.ToSummaryRows(materialized.CreatedMatches)
        };
        var view = StructureViewAssembler.Assemble(
            situation.Competition,
            situation.Stages,
            matchSummariesByStage: summaries);
        var revert = view.Stages.Single().Lifecycle!.RevertTransition;

        revert!.Enabled.Should().BeFalse();
        revert.DisabledReasonCode.Should().Be(StructureStageLifecycleAssembler.DisabledProductionPresent);
    }
}

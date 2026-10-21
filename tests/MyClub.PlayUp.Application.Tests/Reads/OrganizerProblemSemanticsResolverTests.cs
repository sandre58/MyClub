// -----------------------------------------------------------------------
// <copyright file="OrganizerProblemSemanticsResolverTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class OrganizerProblemSemanticsResolverTests
{
    [Fact]
    public void OpenMatches_complete_transition_maps_gate_and_blocked_action()
    {
        var problem = OrganizerProblemSemanticsResolver.TryFromLifecycleTransition(
            OverviewAssembler.ActionCompleteStage,
            CompleteStage.ReasonOpenMatches,
            null);

        problem.Should().NotBeNull();
        problem.CanonCode.Should().Be(CompleteStage.ReasonOpenMatches);
        problem.SemanticLevel.Should().Be(OrganizerProblemSemanticLevels.Gate);
        problem.BlockedActions.Should().ContainSingle().Which.Should()
            .Be(OverviewAssembler.ActionCompleteStage);
    }

    [Fact]
    public void StructureGraphInvalid_prepare_uses_first_graph_gate_issue_as_canon()
    {
        var problem = OrganizerProblemSemanticsResolver.TryFromLifecycleTransition(
            OverviewAssembler.ActionPrepareStage,
            StructureStageLifecycleAssembler.DisabledStructureGraphInvalid,
            [CompetitionFlowAnalyzer.CycleDetected, CompetitionFlowAnalyzer.SlotFeedInvalid]);

        problem.Should().NotBeNull();
        problem.CanonCode.Should().Be(CompetitionFlowAnalyzer.CycleDetected);
        problem.TransportCode.Should().Be(StructureStageLifecycleAssembler.DisabledStructureGraphInvalid);
        problem.BlockedActions.Should().Contain(OverviewAssembler.ActionPrepareStage);
    }

    [Fact]
    public void QualificationPending_attention_maps_attention_and_suggested_action()
    {
        var problem = OrganizerProblemSemanticsResolver.TryFromAttentionSource(
            NeedsAttentionAssembler.SourceQualificationPending);

        problem.Should().NotBeNull();
        problem.SemanticLevel.Should().Be(OrganizerProblemSemanticLevels.Attention);
        problem.BlockedActions.Should().BeEmpty();
        problem.SuggestedActionCode.Should().Be(OverviewAssembler.ActionApplyQualification);
    }

    [Fact]
    public void CycleDetected_structure_issue_maps_prepare_gate()
    {
        var list = OrganizerProblemSemanticsResolver.CollectStructureIssueSemantics(
            [CompetitionFlowAnalyzer.CycleDetected]);

        list.Should().ContainSingle();
        list[0].CanonCode.Should().Be(CompetitionFlowAnalyzer.CycleDetected);
        list[0].SemanticLevel.Should().Be(OrganizerProblemSemanticLevels.Gate);
    }
}

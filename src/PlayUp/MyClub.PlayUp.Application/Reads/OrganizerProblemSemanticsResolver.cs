// -----------------------------------------------------------------------
// <copyright file="OrganizerProblemSemanticsResolver.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Maps existing read-model codes to Step 2 semantics (pilot trio + shared lifecycle gates).
/// </summary>
public static class OrganizerProblemSemanticsResolver
{
    public static OrganizerProblemReadDto? TryFromLifecycleTransition(
        string? transitionCode,
        string? disabledReasonCode,
        IReadOnlyList<string>? structureIssues)
    {
        if (string.IsNullOrEmpty(transitionCode) || string.IsNullOrEmpty(disabledReasonCode))
        {
            return null;
        }

        switch (transitionCode)
        {
            case OverviewAssembler.ActionCompleteStage
                when disabledReasonCode == CompleteStage.ReasonOpenMatches:
                return new OrganizerProblemReadDto(
                    CompleteStage.ReasonOpenMatches,
                    OrganizerProblemSemanticLevels.Gate,
                    [OverviewAssembler.ActionCompleteStage],
                    TransportCode: CompleteStage.ReasonOpenMatches);
            case OverviewAssembler.ActionPrepareStage
                when disabledReasonCode == StructureStageLifecycleAssembler.DisabledStructureGraphInvalid:
                {
                    var canon = ResolveFirstGraphGateIssue(structureIssues)
                                ?? StructureViewAssembler.BlockerStructureGraphInvalid;
                    return new OrganizerProblemReadDto(
                        canon,
                        OrganizerProblemSemanticLevels.Gate,
                        [OverviewAssembler.ActionPrepareStage],
                        TransportCode: StructureStageLifecycleAssembler.DisabledStructureGraphInvalid);
                }

            case OverviewAssembler.ActionPrepareStage when disabledReasonCode == CompetitionFlowAnalyzer.CycleDetected:
                return PrepareGate(CycleDetectedCanon, disabledReasonCode);
            case OverviewAssembler.ActionPrepareStage
                when disabledReasonCode == CompetitionFlowAnalyzer.OverCapacityEdge:
                return PrepareGate(CompetitionFlowAnalyzer.OverCapacityEdge, disabledReasonCode);
            default:
                return null;
        }
    }

    public static OrganizerProblemReadDto? TryFromAttentionSource(string source) =>
        source switch
        {
            NeedsAttentionAssembler.SourceQualificationPending => new OrganizerProblemReadDto(
                NeedsAttentionAssembler.SourceQualificationPending,
                OrganizerProblemSemanticLevels.Attention,
                [],
                TransportCode: NeedsAttentionAssembler.SourceQualificationPending,
                SuggestedActionCode: OverviewAssembler.ActionApplyQualification),
            StructurePresentedStageDiagnostics.PresentedAwaitingInboundQualification =>
                new OrganizerProblemReadDto(
                    StructurePresentedStageDiagnostics.PresentedAwaitingInboundQualification,
                    OrganizerProblemSemanticLevels.Attention,
                    [],
                    TransportCode: StructurePresentedStageDiagnostics.PresentedAwaitingInboundQualification),
            StructureStageLifecycleAssembler.DisabledIncompleteEntryRound
                or StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations
                or StructureStageLifecycleAssembler.DisabledNoExploitableInitialConfrontations
                or StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete
                or StructureStageLifecycleAssembler.DisabledGroupsNotReady =>
                TryFromStartTransitionBlocker(source),
            _ => null
        };

    public static OrganizerProblemReadDto? TryFromStartTransitionBlocker(string disabledReasonCode) =>
        disabledReasonCode switch
        {
            StructureStageLifecycleAssembler.DisabledIncompleteEntryRound =>
                new OrganizerProblemReadDto(
                    disabledReasonCode,
                    OrganizerProblemSemanticLevels.Gate,
                    [OverviewAssembler.ActionStartStage],
                    TransportCode: disabledReasonCode),
            StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations
                or StructureStageLifecycleAssembler.DisabledNoExploitableInitialConfrontations
                or StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete
                or StructureStageLifecycleAssembler.DisabledGroupsNotReady =>
                new OrganizerProblemReadDto(
                    disabledReasonCode,
                    OrganizerProblemSemanticLevels.Gate,
                    [OverviewAssembler.ActionStartStage],
                    TransportCode: disabledReasonCode),
            _ => null
        };

    public static OrganizerProblemReadDto? TryFromStructureIssue(string issueCode) =>
        issueCode == CompetitionFlowAnalyzer.CycleDetected
            ? new OrganizerProblemReadDto(
                CycleDetectedCanon,
                OrganizerProblemSemanticLevels.Gate,
                [OverviewAssembler.ActionPrepareStage],
                TransportCode: issueCode)
            : null;

    public static IReadOnlyList<OrganizerProblemReadDto> CollectStructureIssueSemantics(
        IReadOnlyList<string>? structureIssues) =>
        structureIssues is not { Count: > 0 }
            ? []
            : structureIssues.Distinct(StringComparer.Ordinal).Select(TryFromStructureIssue)
                .OfType<OrganizerProblemReadDto>().ToList();

    private const string CycleDetectedCanon = CompetitionFlowAnalyzer.CycleDetected;

    private static OrganizerProblemReadDto PrepareGate(string canon, string transport) =>
        new(
            canon,
            OrganizerProblemSemanticLevels.Gate,
            [OverviewAssembler.ActionPrepareStage],
            TransportCode: transport);

    private static string? ResolveFirstGraphGateIssue(IReadOnlyList<string>? structureIssues) =>
        structureIssues?.FirstOrDefault(CompetitionFlowAnalyzer.IsStructureGraphGateIssue);
}

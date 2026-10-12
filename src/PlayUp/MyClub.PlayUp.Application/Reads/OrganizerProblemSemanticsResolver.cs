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

        if (transitionCode == OverviewAssembler.ActionCompleteStage
            && disabledReasonCode == CompleteStage.ReasonOpenMatches)
        {
            return new OrganizerProblemReadDto(
                CompleteStage.ReasonOpenMatches,
                OrganizerProblemSemanticLevels.Gate,
                [OverviewAssembler.ActionCompleteStage],
                TransportCode: CompleteStage.ReasonOpenMatches);
        }

        if (transitionCode == OverviewAssembler.ActionPrepareStage)
        {
            if (disabledReasonCode == StructureStageLifecycleAssembler.DisabledStructureGraphInvalid)
            {
                var canon = ResolveFirstGraphGateIssue(structureIssues)
                    ?? StructureViewAssembler.BlockerStructureGraphInvalid;
                return new OrganizerProblemReadDto(
                    canon,
                    OrganizerProblemSemanticLevels.Gate,
                    [OverviewAssembler.ActionPrepareStage],
                    TransportCode: StructureStageLifecycleAssembler.DisabledStructureGraphInvalid);
            }

            if (disabledReasonCode == CompetitionFlowAnalyzer.CycleDetected)
            {
                return PrepareGate(CycleDetectedCanon, disabledReasonCode);
            }

            if (disabledReasonCode == CompetitionFlowAnalyzer.OverCapacityEdge)
            {
                return PrepareGate(CompetitionFlowAnalyzer.OverCapacityEdge, disabledReasonCode);
            }
        }

        return null;
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
            _ => null
        };

    public static OrganizerProblemReadDto? TryFromStructureIssue(string issueCode)
    {
        if (issueCode == CompetitionFlowAnalyzer.CycleDetected)
        {
            return new OrganizerProblemReadDto(
                CycleDetectedCanon,
                OrganizerProblemSemanticLevels.Gate,
                [OverviewAssembler.ActionPrepareStage],
                TransportCode: issueCode);
        }

        return null;
    }

    public static IReadOnlyList<OrganizerProblemReadDto> CollectStructureIssueSemantics(
        IReadOnlyList<string>? structureIssues)
    {
        if (structureIssues is not { Count: > 0 })
        {
            return [];
        }

        var list = new List<OrganizerProblemReadDto>();
        foreach (var code in structureIssues.Distinct(StringComparer.Ordinal))
        {
            var semantics = TryFromStructureIssue(code);
            if (semantics is not null)
            {
                list.Add(semantics);
            }
        }

        return list;
    }

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

// -----------------------------------------------------------------------
// <copyright file="WorkspaceSummaryAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles a minimal WorkspaceSummary for Accueil (Slice 1 + Slice 6).
/// </summary>
/// <remarks>
/// Next action is a Read stub. AttentionCount / completion fields are derived — never Domain.
/// </remarks>
public static class WorkspaceSummaryAssembler
{
    /// <summary>Machine code: continue preparation in Organisation hub.</summary>
    public const string ContinueOrganisationCode = "ContinueOrganisation";

    /// <summary>Machine code: competition is ready for Normal completion.</summary>
    public const string CompleteCompetitionCode = "CompleteCompetition";

    /// <summary>
    /// Builds the workspace summary.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="attentionCount">Derived Needs Attention count.</param>
    /// <param name="analysis">Optional completion analysis (Running/Suspended).</param>
    /// <returns>Workspace summary DTO.</returns>
    public static WorkspaceSummaryDto Assemble(
        Competition competition,
        int attentionCount = 0,
        CompletionAnalysis? analysis = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentOutOfRangeException.ThrowIfNegative(attentionCount);

        var canCompleteNormally = analysis?.CanCompleteNormally ?? false;
        var blockers = analysis?.Reasons.Select(reason => reason.Code).ToArray() ?? [];
        var (code, label) = ResolveNextStub(competition.Status, canCompleteNormally);

        return new WorkspaceSummaryDto(
            competition.Id.Value,
            competition.Name.Value,
            competition.Status,
            code,
            label,
            attentionCount,
            competition.CompletionMode,
            canCompleteNormally,
            blockers);
    }

    private static (string? Code, string? Label) ResolveNextStub(
        CompetitionStatus status,
        bool canCompleteNormally) =>
        status switch
        {
            CompetitionStatus.Draft => (
                ContinueOrganisationCode,
                "Continuer la préparation"),
            CompetitionStatus.Ready => (
                ContinueOrganisationCode,
                "Continuer la préparation"),
            CompetitionStatus.Running when canCompleteNormally => (
                CompleteCompetitionCode,
                "Clôturer la compétition"),
            CompetitionStatus.Suspended when canCompleteNormally => (
                CompleteCompetitionCode,
                "Clôturer la compétition"),
            CompetitionStatus.Running => (
                "OpenMatches",
                "Voir les matchs"),
            CompetitionStatus.Suspended => (
                "OpenMatches",
                "Voir les matchs"),
            CompetitionStatus.Completed => (
                "OpenConsultation",
                "Consulter les résultats"),
            CompetitionStatus.Archived => (
                "OpenConsultation",
                "Consulter l’archive"),
            _ => (null, null)
        };
}

// -----------------------------------------------------------------------
// <copyright file="WorkspaceSummaryAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles a minimal WorkspaceSummary for Accueil (Slice 1).
/// </summary>
/// <remarks>
/// Next action is a Read stub. AttentionCount is derived via Needs Attention (Slice 5).
/// </remarks>
public static class WorkspaceSummaryAssembler
{
    /// <summary>Machine code: continue preparation in Organisation hub.</summary>
    public const string ContinueOrganisationCode = "ContinueOrganisation";

    /// <summary>
    /// Builds the workspace summary.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="attentionCount">Derived Needs Attention count.</param>
    /// <returns>Workspace summary DTO.</returns>
    public static WorkspaceSummaryDto Assemble(Competition competition, int attentionCount = 0)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentOutOfRangeException.ThrowIfNegative(attentionCount);

        var (code, label) = ResolveNextStub(competition.Status);
        return new WorkspaceSummaryDto(
            competition.Id.Value,
            competition.Name.Value,
            competition.Status,
            code,
            label,
            attentionCount);
    }

    private static (string? Code, string? Label) ResolveNextStub(CompetitionStatus status) =>
        status switch
        {
            CompetitionStatus.Draft => (
                ContinueOrganisationCode,
                "Continuer la préparation"),
            CompetitionStatus.Ready => (
                ContinueOrganisationCode,
                "Continuer la préparation"),
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

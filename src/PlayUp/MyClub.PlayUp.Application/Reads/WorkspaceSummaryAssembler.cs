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
/// Next action is a Read stub: Draft → ContinueOrganisation. Richer next / attention / completion
/// arrive in later slices — not Domain properties.
/// </remarks>
public static class WorkspaceSummaryAssembler
{
    /// <summary>Machine code: continue preparation in Organisation hub.</summary>
    public const string ContinueOrganisationCode = "ContinueOrganisation";

    /// <summary>
    /// Builds the Slice 1 workspace summary.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <returns>Workspace summary DTO.</returns>
    public static WorkspaceSummaryDto Assemble(Competition competition)
    {
        ArgumentNullException.ThrowIfNull(competition);

        var (code, label) = ResolveNextStub(competition.Status);
        return new WorkspaceSummaryDto(
            competition.Id.Value,
            competition.Name.Value,
            competition.Status,
            code,
            label,
            AttentionCount: 0);
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

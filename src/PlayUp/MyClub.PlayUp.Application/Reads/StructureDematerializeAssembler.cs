// -----------------------------------------------------------------------
// <copyright file="StructureDematerializeAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Structure workspace projection for phase dematerialization (sibling of materialize).
/// </summary>
internal static class StructureDematerializeAssembler
{
    /// <summary>
    /// Builds the dematerialize facet, or <see langword="null"/> when not exposed for this stage.
    /// </summary>
    public static StructureDematerializeDto? Assemble(
        Competition competition,
        Stage stage,
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matchSummaries);

        var gates = matchSummaries.Select(DematerializationMatchGate.FromSummary).ToArray();
        var capability = DematerializationCapability.Evaluate(competition, stage, gates);
        return !capability.IsExposed || capability.Scope is null
            ? null
            : new StructureDematerializeDto(
            capability.Enabled,
            capability.DisabledReason,
            capability.Scope);
    }
}

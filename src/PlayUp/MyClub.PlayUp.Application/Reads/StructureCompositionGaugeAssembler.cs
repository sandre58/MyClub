// -----------------------------------------------------------------------
// <copyright file="StructureCompositionGaugeAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Composition band — resolved Qual/Prog seats or roster size (Structure GET SoT).
/// Withdrawn entries stay in <see cref="StructureStageHubSummaryDto.CompositionEntryCount"/>; not split here.
/// </summary>
internal static class StructureCompositionGaugeAssembler
{
    internal static int ResolveResolvedSeatCount(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        int compositionMemberCount,
        StructureInboundCompositionDto? inbound,
        int? placesN)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var isRoot = StructureStageInboundTopology.IsRootComposition(stage, competitionStages);
        var resolved = !isRoot && inbound != null
            ? inbound.ResolvedCount
            : compositionMemberCount;

        if (placesN is > 0 && resolved > placesN.Value)
        {
            resolved = placesN.Value;
        }

        return resolved;
    }
}

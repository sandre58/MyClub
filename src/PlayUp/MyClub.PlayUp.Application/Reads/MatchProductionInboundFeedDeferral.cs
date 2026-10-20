// -----------------------------------------------------------------------
// <copyright file="MatchProductionInboundFeedDeferral.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Match production while Qual/Prog inbound seats are not yet resolved (upstream matches pending).
/// </summary>
internal static class MatchProductionInboundFeedDeferral
{
    internal static bool IsAwaitingResolution(
        StructureInboundCompositionDto? inbound,
        int determinedConfrontationCount) =>
        inbound is { ConfiguredVolume: > 0 }
        && determinedConfrontationCount <= 0
        && inbound.ResolvedCount < inbound.ConfiguredVolume;
}

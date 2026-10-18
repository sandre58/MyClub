// -----------------------------------------------------------------------
// <copyright file="StructureDrawCreateLaunchGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Read-side preflight for starting a new draw execution (Structure hub RunDraw).
/// Composes existing command guards — not a SPA port of <c>resolveStageDrawCreateGate</c>.
/// </summary>
public static class StructureDrawCreateLaunchGate
{
    internal const string BlockDrawRulesMissing = "DrawRulesMissing";
    internal const string BlockActiveDrawPresent = "ActiveDrawPresent";
    internal const string BlockUnsupportedFormat = "UnsupportedFormat";
    internal const string BlockEmptyCompositionPool = "EmptyCompositionPool";
    internal const string BlockUpstreamCompositionPoolEmpty = "UpstreamCompositionPoolEmpty";
    internal const string BlockBelowMinimumTeams = "BelowMinimumTeams";
    internal const string BlockMissingPots = "MissingPots";
    internal const string BlockGroupShapeInvalid = "GroupShapeInvalid";
    internal const string BlockPlacesCountMismatch = "PlacesCountMismatch";
    internal const string BlockDirectAssignmentPresent = "DirectAssignmentPresent";
    internal const string BlockBracketPlacesOccupied = "BracketPlacesOccupied";
    internal const string BlockOperationalExitEdit = "OperationalExitEditBlocked";
    internal const string BlockDrawObsoleteForConsumption = "DrawObsoleteForConsumption";
    internal const string BlockBadgeNotToLaunch = "BadgeNotToLaunch";

    /// <summary>
    /// Evaluates whether a new draw may be launched on this phase snapshot.
    /// </summary>
    public static StructureDrawCreateLaunchGateResult Evaluate(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        StructureStageHubSummaryDto hub,
        StructureDrawExecutionBadge? badge,
        IReadOnlyList<MatchSummaryRow>? matchSummaries)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(hub);
        _ = matchSummaries;

        if (!hub.HasDrawRules)
        {
            return Deny(BlockDrawRulesMissing);
        }

        if (badge is not StructureDrawExecutionBadge.ToLaunch)
        {
            return Deny(BlockBadgeNotToLaunch);
        }

        if (stage.Draws.Any(draw => draw.Status != DrawStatus.Cancelled))
        {
            return Deny(BlockActiveDrawPresent);
        }

        if (!TryResolveDrawKind(hub.FormatKind, out var kind))
        {
            return Deny(BlockUnsupportedFormat);
        }

        if (BlocksUpstreamCompositionPool(hub))
        {
            return Deny(BlockUpstreamCompositionPoolEmpty);
        }

        if (hub.CompositionEntryCount <= 0)
        {
            return Deny(BlockEmptyCompositionPool);
        }

        var minimumTeams = competition.Regulation.EntryRules.MinimumTeams;
        if (hub.CompositionEntryCount < minimumTeams)
        {
            return Deny(BlockBelowMinimumTeams);
        }

        if (kind == DrawResolutionKind.Group)
        {
            var pots = hub.NumberOfPots;
            var groups = hub.GroupCount;
            if (pots is null or < 2)
            {
                return Deny(BlockMissingPots);
            }

            if (groups < 1 || hub.CompositionEntryCount != pots.Value * groups)
            {
                return Deny(BlockGroupShapeInvalid);
            }
        }
        else
        {
            var places = hub.CompositionCapacity;
            if (places is null or <= 0 || hub.CompositionEntryCount != places.Value)
            {
                return Deny(BlockPlacesCountMismatch);
            }

            if (hub.DirectAssignmentCount > 0)
            {
                return Deny(BlockDirectAssignmentPresent);
            }
        }

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(stage, competitionStages, kind);
        if (facts.BlockingPlaceCount > 0)
        {
            return Deny(BlockBracketPlacesOccupied);
        }

        if (StructureDrawConsumptionGate.StageHasObsoleteDrawForConsumption(stage))
        {
            return Deny(BlockDrawObsoleteForConsumption);
        }

        if (!TryAllowOperationalDrawEdit(stage))
        {
            return Deny(BlockOperationalExitEdit);
        }

        return Allow();
    }

    private static bool BlocksUpstreamCompositionPool(StructureStageHubSummaryDto hub)
    {
        if (hub.IsRootComposition)
        {
            return false;
        }

        if (hub.CompositionEntryCount <= 0)
        {
            return true;
        }

        if (hub.InboundComposition is { ConfiguredVolume: > 0 } inbound
            && inbound.ResolvedCount < inbound.ConfiguredVolume)
        {
            return true;
        }

        return false;
    }

    private static bool TryAllowOperationalDrawEdit(Stage stage)
    {
        try
        {
            StructureOperationalExitEditGuard.Ensure(stage, upstreamMatches: null, "Draw");
            return true;
        }
        catch (ApplicationFailureException)
        {
            return false;
        }
    }

    private static bool TryResolveDrawKind(StructureFormatKind? format, out DrawResolutionKind kind)
    {
        switch (format)
        {
            case StructureFormatKind.Cup:
                kind = DrawResolutionKind.Slot;
                return true;
            case StructureFormatKind.Groups:
                kind = DrawResolutionKind.Group;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    private static StructureDrawCreateLaunchGateResult Allow() =>
        new(true, null);

    private static StructureDrawCreateLaunchGateResult Deny(string code) =>
        new(false, code);
}

/// <summary>Result of <see cref="StructureDrawCreateLaunchGate.Evaluate"/>.</summary>
/// <param name="Allowed">Whether launch is permitted.</param>
/// <param name="BlockReasonCode">Stable internal code when blocked (not exposed on HTTP for PageHead).</param>
public readonly record struct StructureDrawCreateLaunchGateResult(bool Allowed, string? BlockReasonCode);

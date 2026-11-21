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
    internal const string BlockNoFreeSlot = "NoFreeSlot";
    internal const string BlockNotEnoughTeams = "NotEnoughTeams";
    internal const string BlockTooManyTeams = "TooManyTeams";
    internal const string BlockOperationalExitEdit = "OperationalExitEditBlocked";
    internal const string BlockDrawObsoleteForConsumption = "DrawObsoleteForConsumption";
    internal const string BlockBadgeNotToLaunch = "BadgeNotToLaunch";

    /// <summary>
    /// Evaluates whether a new draw may be launched on this phase snapshot.
    /// </summary>
    public static StructureDrawCreateLaunchDto Evaluate(
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

        GroupDrawLaunchDecision? groupDecision = null;
        if (kind == DrawResolutionKind.Group)
        {
            groupDecision = GroupDrawLaunchRules.Evaluate(stage, competitionStages);
            if (groupDecision.Code == ApplicationErrorCodes.DrawGroupSeatsReservedByInboundFeed)
            {
                return Deny(groupDecision.Code, groupDecision.Reasons);
            }
        }

        if (BlocksUpstreamCompositionPool(hub)
            && !(kind == DrawResolutionKind.Group && hub.CompositionEntryCount > 0))
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
            if (!groupDecision!.IsAllowed)
            {
                return Deny(MapGroupRuleCode(groupDecision.Code!), groupDecision.Reasons);
            }
        }
        else
        {
            var scope = SlotPartialDrawPlanner.Compose(stage, competitionStages);
            if (!scope.IsDrawable)
            {
                return Deny(scope.Refusal switch
                {
                    SlotPartialDrawRefusal.NoFreeSlot => BlockNoFreeSlot,
                    SlotPartialDrawRefusal.NotEnoughTeams => BlockNotEnoughTeams,
                    SlotPartialDrawRefusal.TooManyTeams => BlockTooManyTeams,
                    SlotPartialDrawRefusal.None => throw new InvalidOperationException(
                        $"Unexpected partial draw refusal '{scope.Refusal}'."),
                    _ => throw new InvalidOperationException(
                        $"Unexpected partial draw refusal '{scope.Refusal}'.")
                });
            }
        }

        return StructureDrawConsumptionGate.StageHasObsoleteDrawForConsumption(stage)
            ? Deny(BlockDrawObsoleteForConsumption)
            : !TryAllowOperationalDrawEdit(stage)
                ? Deny(BlockOperationalExitEdit)
                : Allow();
    }

    /// <summary>
    /// Gets the Groups gate reason and the server code a command returns for the same cause.
    /// The gate follows <see cref="DrawInputsIntent.Default"/>: the shared launch rule only.
    /// A legal occupied grid is allowed. Create with <see cref="DrawInputsIntent.Rerun"/> still
    /// refuses that grid with <see cref="ApplicationErrorCodes.DrawLaunchGridNotVacant"/>.
    /// </summary>
    internal static IReadOnlyList<(string GateCode, string ServerCode)> GroupReasonParity { get; } =
    [
        (BlockMissingPots, ApplicationErrorCodes.DrawPotsMissing),
        (BlockGroupShapeInvalid, ApplicationErrorCodes.DrawGroupShapeInvalid),
        (ApplicationErrorCodes.DrawGroupSeatsReservedByInboundFeed, ApplicationErrorCodes.DrawGroupSeatsReservedByInboundFeed),
        (ApplicationErrorCodes.DrawFixedPotConflict, ApplicationErrorCodes.DrawFixedPotConflict),
        (ApplicationErrorCodes.DrawFixedOutsideComposition, ApplicationErrorCodes.DrawFixedOutsideComposition),
        (ApplicationErrorCodes.DrawFixedExceedsCapacity, ApplicationErrorCodes.DrawFixedExceedsCapacity),
        (BlockActiveDrawPresent, StageErrorCodes.NonCancelledDrawAlreadyExists),
        (BlockDrawObsoleteForConsumption, ApplicationErrorCodes.DrawObsoleteForConsumption),
    ];

    private static string MapGroupRuleCode(string code) =>
        code switch
        {
            ApplicationErrorCodes.DrawPotsMissing => BlockMissingPots,
            ApplicationErrorCodes.DrawGroupShapeInvalid => BlockGroupShapeInvalid,
            _ => code,
        };

    private static bool BlocksUpstreamCompositionPool(StructureStageHubSummaryDto hub) => !hub.IsRootComposition && (hub.CompositionEntryCount <= 0 || (hub.InboundComposition is { ConfiguredVolume: > 0 } inbound
        && inbound.ResolvedCount < inbound.ConfiguredVolume));

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

    private static StructureDrawCreateLaunchDto Allow() =>
        new(true, null);

    private static StructureDrawCreateLaunchDto Deny(string code, IReadOnlyList<string>? detail = null) =>
        new(false, code, detail is { Count: > 0 } ? detail : null);
}

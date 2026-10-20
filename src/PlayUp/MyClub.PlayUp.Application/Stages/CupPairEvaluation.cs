// -----------------------------------------------------------------------
// <copyright file="CupPairEvaluation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

internal enum CupOccupationChannel
{
    Missing,
    Direct,
    Draw,
    Progression,
    Qualification,
    MultipleFeeds,
    InvalidFeed
}

internal enum CupPairBlockReason
{
    SlotEmpty,
    OccupantsNotDistinct,
    FeedInvalid,
    ProjectionOnly,
    DestinationNotSatisfied,
    ResolutionAmbiguous,
    OccupantConflict,
    AlreadyMaterialized
}

internal readonly record struct CupPairEvaluationInput(
    Competition Competition,
    Stage Stage,
    BracketPair Pair,
    IReadOnlyList<Stage> CompetitionStages,
    IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? MatchSummariesByStage,
    bool PhaseMutable);

internal readonly record struct CupPairEvaluationResult(
    bool DeterminedForDisplay,
    bool DeterminedForMaterialize,
    bool Materialized,
    bool EligibleForMaterialize,
    IReadOnlyList<CupPairBlockReason> MaterializeBlockReasons,
    CupOccupationChannel? SideAChannel,
    CupOccupationChannel? SideBChannel,
    string? MaterializeErrorCode);

internal static class CupPairEvaluation
{
    internal static CupPairEvaluationResult Evaluate(CupPairEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(input.Competition);
        ArgumentNullException.ThrowIfNull(input.Stage);
        ArgumentNullException.ThrowIfNull(input.CompetitionStages);

        var pair = input.Pair;
        var context = new CupBracketPairMaterialization.ResolutionContext(
            input.CompetitionStages,
            input.MatchSummariesByStage);

        var determinedForDisplay = CupBracketPairMaterialization.TryResolvePairEntries(
            input.Stage,
            pair,
            context,
            out _,
            out _);

        var slotA = input.Stage.FindSlot(pair.SlotAKey);
        var slotB = input.Stage.FindSlot(pair.SlotBKey);
        var persistedA = slotA?.EntryId;
        var persistedB = slotB?.EntryId;
        var determinedForMaterialize = persistedA is { } home
                                     && persistedB is { } away
                                     && !home.Equals(away);

        var materialized = CupBracketPairMaterialization.IsConfrontationMaterialized(
            input.Stage,
            pair,
            context);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(input.Stage, input.CompetitionStages);
        var sideA = EvaluateSlotSide(input, snapshot, pair.SlotAKey);
        var sideB = EvaluateSlotSide(input, snapshot, pair.SlotBKey);

        var blockers = new List<CupPairBlockReason>();
        string? errorCode = null;

        if (materialized)
        {
            blockers.Add(CupPairBlockReason.AlreadyMaterialized);
        }

        if (!determinedForMaterialize)
        {
            if (persistedA is null || persistedB is null)
            {
                blockers.Add(CupPairBlockReason.SlotEmpty);
                errorCode = PreferError(errorCode, ApplicationErrorCodes.MaterializationFailure);
            }
            else if (persistedA.Value.Equals(persistedB.Value))
            {
                blockers.Add(CupPairBlockReason.OccupantsNotDistinct);
                errorCode = PreferError(errorCode, ApplicationErrorCodes.MaterializationFailure);
            }
        }

        MergeSideBlockers(sideA, blockers, ref errorCode);
        MergeSideBlockers(sideB, blockers, ref errorCode);

        var channelEligible = sideA.Eligible && sideB.Eligible;
        var eligible = input.PhaseMutable
                       && !materialized
                       && determinedForMaterialize
                       && channelEligible;

        return new CupPairEvaluationResult(
            determinedForDisplay,
            determinedForMaterialize,
            materialized,
            eligible,
            blockers,
            sideA.Channel,
            sideB.Channel,
            errorCode);
    }

    internal static string ResolveMaterializeErrorCode(CupPairEvaluationResult result) =>
        result.MaterializeErrorCode ?? ApplicationErrorCodes.MaterializationFailure;

    private static void MergeSideBlockers(
        SlotSideEvaluation side,
        List<CupPairBlockReason> blockers,
        ref string? errorCode)
    {
        if (side.BlockReason is { } reason)
        {
            blockers.Add(reason);
        }

        if (side.ErrorCode is not null)
        {
            errorCode = PreferError(errorCode, side.ErrorCode);
        }
    }

    private static string PreferError(string? current, string candidate)
    {
        if (current is null)
        {
            return candidate;
        }

        if (candidate == ApplicationErrorCodes.MaterializationOccupantConflict
            || current == ApplicationErrorCodes.MaterializationOccupantConflict)
        {
            return ApplicationErrorCodes.MaterializationOccupantConflict;
        }

        if (candidate == ApplicationErrorCodes.ProgressionNotAppliedForMaterialization
            && current == ApplicationErrorCodes.MaterializationFailure)
        {
            return ApplicationErrorCodes.ProgressionNotAppliedForMaterialization;
        }

        return current;
    }

    private readonly record struct SlotSideEvaluation(
        CupOccupationChannel Channel,
        bool Eligible,
        CupPairBlockReason? BlockReason,
        string? ErrorCode);

    private static SlotSideEvaluation EvaluateSlotSide(
        CupPairEvaluationInput input,
        SlotFeedSnapshot snapshot,
        string slotKey)
    {
        var resolution = SlotFeedResolver.Resolve(snapshot, slotKey);
        var channel = MapChannel(resolution);

        if (channel is CupOccupationChannel.MultipleFeeds or CupOccupationChannel.InvalidFeed)
        {
            return new SlotSideEvaluation(
                channel,
                false,
                CupPairBlockReason.FeedInvalid,
                ApplicationErrorCodes.SlotFeedsInvalid);
        }

        var occupant = input.Stage.FindSlot(slotKey)?.EntryId;

        return channel switch
        {
            CupOccupationChannel.Missing => EvaluateMissingChannel(occupant),
            CupOccupationChannel.Direct => EvaluateDirectChannel(resolution, occupant),
            CupOccupationChannel.Draw => EvaluateDrawChannel(input.Stage, resolution, slotKey, occupant),
            CupOccupationChannel.Progression => EvaluateProgressionChannel(input, resolution, slotKey, occupant),
            CupOccupationChannel.Qualification => EvaluateQualificationChannel(input, resolution, slotKey, occupant),
            _ => new SlotSideEvaluation(channel, false, CupPairBlockReason.FeedInvalid, ApplicationErrorCodes.SlotFeedsInvalid)
        };
    }

    private static SlotSideEvaluation EvaluateMissingChannel(EntryId? occupant) =>
        occupant is null
            ? new SlotSideEvaluation(
                CupOccupationChannel.Missing,
                false,
                CupPairBlockReason.SlotEmpty,
                ApplicationErrorCodes.MaterializationFailure)
            : new SlotSideEvaluation(CupOccupationChannel.Missing, true, null, null);

    private static SlotSideEvaluation EvaluateDirectChannel(SlotFeedResolution resolution, EntryId? occupant)
    {
        var expected = resolution.Source!.Direct!.ConfiguredEntryId;
        if (occupant is null)
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Direct,
                false,
                CupPairBlockReason.SlotEmpty,
                ApplicationErrorCodes.MaterializationFailure);
        }

        if (!occupant.Value.Equals(expected))
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Direct,
                false,
                CupPairBlockReason.OccupantConflict,
                ApplicationErrorCodes.MaterializationOccupantConflict);
        }

        return new SlotSideEvaluation(CupOccupationChannel.Direct, true, null, null);
    }

    private static SlotSideEvaluation EvaluateDrawChannel(
        Stage stage,
        SlotFeedResolution resolution,
        string slotKey,
        EntryId? occupant)
    {
        var drawId = resolution.Source!.Draw!.DrawId;
        var placement = TryGetDrawPlacementEntry(stage, drawId, slotKey);
        if (placement is null)
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Draw,
                false,
                CupPairBlockReason.ResolutionAmbiguous,
                ApplicationErrorCodes.InboundResolutionAmbiguous);
        }

        if (occupant is null)
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Draw,
                false,
                CupPairBlockReason.SlotEmpty,
                ApplicationErrorCodes.MaterializationFailure);
        }

        if (!occupant.Value.Equals(placement.Value))
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Draw,
                false,
                CupPairBlockReason.OccupantConflict,
                ApplicationErrorCodes.MaterializationOccupantConflict);
        }

        return new SlotSideEvaluation(CupOccupationChannel.Draw, true, null, null);
    }

    private static SlotSideEvaluation EvaluateProgressionChannel(
        CupPairEvaluationInput input,
        SlotFeedResolution resolution,
        string slotKey,
        EntryId? occupant)
    {
        if (!TryResolveProgressionPath(input, resolution, out var sourceStage, out var path))
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Progression,
                false,
                CupPairBlockReason.ResolutionAmbiguous,
                ApplicationErrorCodes.InboundResolutionAmbiguous);
        }

        var replay = InboundFeedSeatResolver.ProjectProgressionEntryFromSourceFixture(
            path!,
            sourceStage!,
            input.MatchSummariesByStage);
        if (replay is null)
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Progression,
                false,
                CupPairBlockReason.ResolutionAmbiguous,
                ApplicationErrorCodes.InboundResolutionAmbiguous);
        }

        if (occupant is null)
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Progression,
                false,
                CupPairBlockReason.ProjectionOnly,
                ApplicationErrorCodes.ProgressionNotAppliedForMaterialization);
        }

        var satisfaction = DestinationSatisfaction.EvaluateProgression(
            input.Stage,
            sourceStage!.Id,
            path!,
            replay.Value);
        if (satisfaction != DestinationSatisfaction.Kind.Satisfied)
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Progression,
                false,
                CupPairBlockReason.DestinationNotSatisfied,
                ApplicationErrorCodes.InboundDestinationNotSatisfied);
        }

        if (!occupant.Value.Equals(replay.Value))
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Progression,
                false,
                CupPairBlockReason.OccupantConflict,
                ApplicationErrorCodes.MaterializationOccupantConflict);
        }

        return new SlotSideEvaluation(CupOccupationChannel.Progression, true, null, null);
    }

    private static SlotSideEvaluation EvaluateQualificationChannel(
        CupPairEvaluationInput input,
        SlotFeedResolution resolution,
        string slotKey,
        EntryId? occupant)
    {
        if (!TryResolveQualificationPath(input, resolution, out var sourceStage, out var path))
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Qualification,
                false,
                CupPairBlockReason.ResolutionAmbiguous,
                ApplicationErrorCodes.InboundResolutionAmbiguous);
        }

        var replay = InboundFeedSeatResolver.ResolveQualificationEntry(
            path!,
            input.Stage,
            sourceStage!,
            input.MatchSummariesByStage);
        if (replay is null)
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Qualification,
                false,
                CupPairBlockReason.ResolutionAmbiguous,
                ApplicationErrorCodes.InboundResolutionAmbiguous);
        }

        if (occupant is null)
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Qualification,
                false,
                CupPairBlockReason.SlotEmpty,
                ApplicationErrorCodes.MaterializationFailure);
        }

        var satisfaction = DestinationSatisfaction.EvaluateQualification(
            input.Stage,
            sourceStage!.Id,
            path!,
            replay.Value);
        if (satisfaction != DestinationSatisfaction.Kind.Satisfied)
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Qualification,
                false,
                CupPairBlockReason.DestinationNotSatisfied,
                ApplicationErrorCodes.InboundDestinationNotSatisfied);
        }

        if (!occupant.Value.Equals(replay.Value))
        {
            return new SlotSideEvaluation(
                CupOccupationChannel.Qualification,
                false,
                CupPairBlockReason.OccupantConflict,
                ApplicationErrorCodes.MaterializationOccupantConflict);
        }

        return new SlotSideEvaluation(CupOccupationChannel.Qualification, true, null, null);
    }

    private static bool TryResolveProgressionPath(
        CupPairEvaluationInput input,
        SlotFeedResolution resolution,
        out Stage? sourceStage,
        out ProgressionPath? path)
    {
        sourceStage = null;
        path = null;
        var progression = resolution.Source?.Progression;
        if (progression is null)
        {
            return false;
        }

        sourceStage = input.CompetitionStages.FirstOrDefault(stage => stage.Id.Equals(progression.SourceStageId))!;
        if (sourceStage is null)
        {
            return false;
        }

        if (sourceStage.Regulation.ProgressionRules is not { } rules)
        {
            return false;
        }

        path = rules.Paths.FirstOrDefault(candidate =>
            string.Equals(candidate.SourcePairKey, progression.SourcePairKey, StringComparison.Ordinal)
            && candidate.Outcome == progression.Outcome
            && string.Equals(candidate.Destination.SlotKey, resolution.SlotKey, StringComparison.Ordinal));
        return path is not null;
    }

    private static bool TryResolveQualificationPath(
        CupPairEvaluationInput input,
        SlotFeedResolution resolution,
        out Stage? sourceStage,
        out QualificationPath? path)
    {
        sourceStage = null;
        path = null;
        var qualification = resolution.Source?.Qualification;
        if (qualification is null)
        {
            return false;
        }

        sourceStage = input.CompetitionStages.FirstOrDefault(stage => stage.Id.Equals(qualification.SourceStageId))!;
        if (sourceStage is null)
        {
            return false;
        }

        if (sourceStage.Regulation.QualificationRules is not { } rules)
        {
            return false;
        }

        path = rules.Paths.FirstOrDefault(candidate =>
            candidate.Order == qualification.PathOrder
            && string.Equals(candidate.Destination.SlotKey, resolution.SlotKey, StringComparison.Ordinal));
        return path is not null;
    }

    private static EntryId? TryGetDrawPlacementEntry(Stage stage, DrawId drawId, string slotKey)
    {
        var draw = stage.Draws.FirstOrDefault(candidate => candidate.Id.Equals(drawId));
        if (draw is null
            || draw.Status != DrawStatus.Published
            || draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return null;
        }

        var placement = draw.Resolution.SlotResults
            .FirstOrDefault(result => string.Equals(result.SlotKey, slotKey, StringComparison.Ordinal));
        return placement?.EntryId;
    }

    private static CupOccupationChannel MapChannel(SlotFeedResolution resolution) =>
        resolution.Status switch
        {
            FeedResolutionStatus.Missing => CupOccupationChannel.Missing,
            FeedResolutionStatus.MultipleFeeds => CupOccupationChannel.MultipleFeeds,
            FeedResolutionStatus.InvalidFeed => CupOccupationChannel.InvalidFeed,
            FeedResolutionStatus.Unique => resolution.Source!.Kind switch
            {
                FeedKind.Direct => CupOccupationChannel.Direct,
                FeedKind.Draw => CupOccupationChannel.Draw,
                FeedKind.Progression => CupOccupationChannel.Progression,
                FeedKind.Qualification => CupOccupationChannel.Qualification,
                _ => CupOccupationChannel.InvalidFeed
            },
            _ => CupOccupationChannel.InvalidFeed
        };

}

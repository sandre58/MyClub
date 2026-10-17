// -----------------------------------------------------------------------
// <copyright file="ProgressionPathProductionParticipationWitness.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages.ProgressionImpact;

/// <summary>
/// WC-2 witness (A-4 / 2c), evaluated by <see cref="StructureProgressionImpactGate"/>.
/// </summary>
internal static class ProgressionPathProductionParticipationWitness
{
    internal enum Verdict
    {
        NotApplicable,
        NotParticipating,
        Participating,
        ParticipationAmbiguous
    }

    internal static Verdict EvaluateTouchedPath(
        ProgressionPath pathBefore,
        Stage sourceStage,
        Stage destinationStage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<MatchParticipantSnapshot> destinationMaterializedParticipants,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? upstreamMatchSummariesByStage)
    {
        ArgumentNullException.ThrowIfNull(pathBefore);
        ArgumentNullException.ThrowIfNull(sourceStage);
        ArgumentNullException.ThrowIfNull(destinationStage);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(destinationMaterializedParticipants);

        return !ProgressionDestinationAttachedProduction.HasAttachedProduction(
                destinationStage,
                destinationMaterializedParticipants)
            ? Verdict.NotApplicable
            : pathBefore.Destination.TargetsSlot
            ? EvaluateSlotParticipation(
                pathBefore,
                sourceStage,
                destinationStage,
                competitionStages,
                destinationMaterializedParticipants,
                upstreamMatchSummariesByStage)
            : pathBefore.Destination.TargetsForm
            ? EvaluateFormParticipation(
                pathBefore,
                sourceStage,
                destinationStage,
                destinationMaterializedParticipants,
                upstreamMatchSummariesByStage)
            : EvaluatePopulationOrGroupParticipation(
            pathBefore,
            sourceStage,
            destinationStage,
            destinationMaterializedParticipants,
            upstreamMatchSummariesByStage,
            pathBefore.Destination.TargetsGroup);
    }

    private static Verdict EvaluateFormParticipation(
        ProgressionPath pathBefore,
        Stage sourceStage,
        Stage destinationStage,
        IReadOnlyList<MatchParticipantSnapshot> destinationMaterializedParticipants,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? upstreamMatchSummariesByStage)
    {
        if (!TryResolveExpectedEntryFromReplay(pathBefore, sourceStage, upstreamMatchSummariesByStage, out var expectedEntry))
        {
            return Verdict.ParticipationAmbiguous;
        }

        var fingerprint = FormPathResolutionKey.FromProgression(sourceStage.Id, pathBefore);
        var resolution = destinationStage.FormPathResolutions
            .FirstOrDefault(candidate => candidate.PathFingerprint == fingerprint);
        return resolution is null
            ? Verdict.ParticipationAmbiguous
            : !resolution.EntryId.Equals(expectedEntry)
            ? Verdict.NotParticipating
            : ProgressionDestinationAttachedProduction.EntryAppearsOnAttachedProduction(
            destinationStage,
            expectedEntry,
            destinationMaterializedParticipants)
            ? Verdict.Participating
            : Verdict.NotParticipating;
    }

    private static Verdict EvaluateSlotParticipation(
        ProgressionPath pathBefore,
        Stage sourceStage,
        Stage destinationStage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<MatchParticipantSnapshot> destinationMaterializedParticipants,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? upstreamMatchSummariesByStage)
    {
        var slotKey = pathBefore.Destination.SlotKey;
        if (string.IsNullOrWhiteSpace(slotKey))
        {
            return Verdict.ParticipationAmbiguous;
        }

        var snapshot = SlotFeedSnapshotAssembler.Assemble(destinationStage, competitionStages);
        var resolution = SlotFeedResolver.Resolve(snapshot, slotKey);
        return resolution is { Status: FeedResolutionStatus.Unique, Source.Kind: FeedKind.Progression }
            ? !InboundFeedSeatResolver.SlotFeedMatchesProgression(
                    destinationStage,
                    competitionStages,
                    sourceStage.Id,
                    pathBefore)
                ? Verdict.NotParticipating
                : !TryResolveExpectedEntryFromReplay(pathBefore, sourceStage, upstreamMatchSummariesByStage, out var expectedEntry)
                ? Verdict.ParticipationAmbiguous
                : ProgressionDestinationAttachedProduction.EntryAppearsOnAttachedProduction(
                destinationStage,
                expectedEntry,
                destinationMaterializedParticipants)
                ? Verdict.Participating
                : Verdict.NotParticipating
            : resolution.Status != FeedResolutionStatus.Unique || !TryResolveExpectedEntryFromReplay(pathBefore, sourceStage, upstreamMatchSummariesByStage, out var replayedEntry)
            ? Verdict.ParticipationAmbiguous
            : ProgressionDestinationAttachedProduction.EntryAppearsOnAttachedProduction(
            destinationStage,
            replayedEntry,
            destinationMaterializedParticipants)
            ? Verdict.Participating
            : Verdict.NotParticipating;
    }

    private static Verdict EvaluatePopulationOrGroupParticipation(
        ProgressionPath pathBefore,
        Stage sourceStage,
        Stage destinationStage,
        IReadOnlyList<MatchParticipantSnapshot> destinationMaterializedParticipants,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? upstreamMatchSummariesByStage,
        bool requireGroupMembership)
    {
        if (!TryResolveExpectedEntryFromReplay(pathBefore, sourceStage, upstreamMatchSummariesByStage, out var expectedEntry))
        {
            return Verdict.ParticipationAmbiguous;
        }

        if (!requireGroupMembership)
        {
            return ProgressionDestinationAttachedProduction.EntryAppearsOnAttachedProduction(
                destinationStage,
                expectedEntry,
                destinationMaterializedParticipants)
                ? Verdict.Participating
                : Verdict.NotParticipating;
        }

        var groupId = pathBefore.Destination.GroupId;
        if (groupId is null)
        {
            return Verdict.ParticipationAmbiguous;
        }

        var group = destinationStage.FindGroup(groupId.Value);
        return group?.EntryIds.Contains(expectedEntry) != true
            ? Verdict.NotParticipating
            : ProgressionDestinationAttachedProduction.EntryAppearsOnAttachedProduction(
            destinationStage,
            expectedEntry,
            destinationMaterializedParticipants)
            ? Verdict.Participating
            : Verdict.NotParticipating;
    }

    private static bool TryResolveExpectedEntryFromReplay(
        ProgressionPath path,
        Stage sourceStage,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage,
        out EntryId expectedEntry)
    {
        var projected = InboundFeedSeatResolver.ProjectProgressionEntryFromSourceFixture(
            path,
            sourceStage,
            matchSummariesByStage);
        if (projected is null)
        {
            expectedEntry = default;
            return false;
        }

        expectedEntry = projected.Value;
        return true;
    }
}

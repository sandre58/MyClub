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

        if (!ProgressionDestinationAttachedProduction.HasAttachedProduction(
                destinationStage,
                destinationMaterializedParticipants))
        {
            return Verdict.NotApplicable;
        }

        if (pathBefore.Destination.TargetsSlot)
        {
            return EvaluateSlotParticipation(
                pathBefore,
                sourceStage,
                destinationStage,
                competitionStages,
                destinationMaterializedParticipants,
                upstreamMatchSummariesByStage);
        }

        if (pathBefore.Destination.TargetsForm)
        {
            return EvaluateFormParticipation(
                pathBefore,
                sourceStage,
                destinationStage,
                destinationMaterializedParticipants,
                upstreamMatchSummariesByStage);
        }

        return EvaluatePopulationOrGroupParticipation(
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
        if (resolution is null)
        {
            return Verdict.ParticipationAmbiguous;
        }

        if (!resolution.EntryId.Equals(expectedEntry))
        {
            return Verdict.NotParticipating;
        }

        return ProgressionDestinationAttachedProduction.EntryAppearsOnAttachedProduction(
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
        if (resolution.Status == FeedResolutionStatus.Unique
            && resolution.Source is not null
            && resolution.Source.Kind == FeedKind.Progression)
        {
            if (!InboundFeedSeatResolver.SlotFeedMatchesProgression(
                    destinationStage,
                    competitionStages,
                    sourceStage.Id,
                    pathBefore))
            {
                return Verdict.NotParticipating;
            }

            if (!TryResolveExpectedEntryFromReplay(pathBefore, sourceStage, upstreamMatchSummariesByStage, out var expectedEntry))
            {
                return Verdict.ParticipationAmbiguous;
            }

            return ProgressionDestinationAttachedProduction.EntryAppearsOnAttachedProduction(
                destinationStage,
                expectedEntry,
                destinationMaterializedParticipants)
                ? Verdict.Participating
                : Verdict.NotParticipating;
        }

        if (resolution.Status != FeedResolutionStatus.Unique)
        {
            return Verdict.ParticipationAmbiguous;
        }

        if (!TryResolveExpectedEntryFromReplay(pathBefore, sourceStage, upstreamMatchSummariesByStage, out var replayedEntry))
        {
            return Verdict.ParticipationAmbiguous;
        }

        return ProgressionDestinationAttachedProduction.EntryAppearsOnAttachedProduction(
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

        if (requireGroupMembership)
        {
            var groupId = pathBefore.Destination.GroupId;
            if (groupId is null)
            {
                return Verdict.ParticipationAmbiguous;
            }

            var group = destinationStage.FindGroup(groupId.Value);
            if (group is null || !group.EntryIds.Contains(expectedEntry))
            {
                return Verdict.NotParticipating;
            }
        }

        return ProgressionDestinationAttachedProduction.EntryAppearsOnAttachedProduction(
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

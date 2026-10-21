// -----------------------------------------------------------------------
// <copyright file="CupInboundSlotEntryResolver.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Cup schematic — resolved occupant on a slot from Qual/Prog feeds when <see cref="Slot.EntryId"/> is not yet synced.
/// </summary>
internal static class CupInboundSlotEntryResolver
{
    internal static EntryId? TryResolve(
        Stage destination,
        string slotKey,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(competitionStages);

        if (destination.FindSlot(slotKey)?.EntryId is { } occupant)
        {
            return occupant;
        }

        foreach (var source in competitionStages)
        {
            if (source.Regulation.QualificationRules is { } qualification)
            {
                foreach (var path in qualification.Paths)
                {
                    if (!TargetsSlot(path.Destination, destination.Id, slotKey))
                    {
                        continue;
                    }

                    var resolved = InboundFeedSeatResolver.ResolveQualificationEntry(
                        path,
                        destination,
                        source,
                        matchSummariesByStage);
                    if (resolved is not null)
                    {
                        return resolved;
                    }
                }
            }

            if (source.Regulation.ProgressionRules is not { } progression)
            {
                continue;
            }

            foreach (var path in progression.Paths)
            {
                if (!TargetsSlot(path.Destination, destination.Id, slotKey))
                {
                    continue;
                }

                var resolved = InboundFeedSeatResolver.ResolveProgressionEntry(
                    path,
                    destination,
                    source,
                    matchSummariesByStage);
                if (resolved is not null)
                {
                    return resolved;
                }
            }
        }

        return null;
    }

    private static bool TargetsSlot(ProgressionDestination destination, StageId stageId, string slotKey) =>
        destination.StageId.Equals(stageId)
        && destination.TargetsSlot
        && string.Equals(destination.SlotKey, slotKey, StringComparison.Ordinal);

    private static bool TargetsSlot(QualificationDestination destination, StageId stageId, string slotKey) =>
        destination.StageId.Equals(stageId)
        && destination.TargetsSlot
        && string.Equals(destination.SlotKey, slotKey, StringComparison.Ordinal);
}

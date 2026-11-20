// -----------------------------------------------------------------------
// <copyright file="ResolutionPerimeSlotAttention.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Per-slot périmé occupant keyed for Needs Attention (slot + qualification path).
/// </summary>
internal readonly record struct ResolutionPerimeSlotAttentionKey(
    Guid DestinationStageId,
    string SlotKey,
    string SourcePathId);

internal static class ResolutionPerimeSlotAttention
{
    internal static HashSet<ResolutionPerimeSlotAttentionKey> Build(IReadOnlyDictionary<Guid, IReadOnlyList<StructureResolutionOccupantDto>> occupantsByStage)
    {
        ArgumentNullException.ThrowIfNull(occupantsByStage);

        var keys = new HashSet<ResolutionPerimeSlotAttentionKey>();
        foreach (var (stageId, occupants) in occupantsByStage)
        {
            foreach (var occupant in occupants)
            {
                if (occupant.State != StructureResolutionFreshness.Perime
                    || occupant.Target != StructureResolutionOccupantTarget.Slot
                    || string.IsNullOrEmpty(occupant.SlotKey))
                {
                    continue;
                }

                keys.Add(
                    new ResolutionPerimeSlotAttentionKey(
                        stageId,
                        occupant.SlotKey,
                        occupant.SourcePathId));
            }
        }

        return keys;
    }

    internal static bool TryMatchQualificationConflict(
        HashSet<ResolutionPerimeSlotAttentionKey> perimeSlots,
        Stage destination,
        QualificationPath path,
        StageId sourceStageId)
    {
        var pathId = ResolutionPathIdentifier.ForQualification(sourceStageId, path);
        if (!path.Destination.TargetsSlot || string.IsNullOrEmpty(path.Destination.SlotKey))
        {
            return false;
        }

        return perimeSlots.Any(key => key.DestinationStageId == destination.Id.Value && string.Equals(key.SlotKey, path.Destination.SlotKey, StringComparison.Ordinal) && string.Equals(key.SourcePathId, pathId, StringComparison.Ordinal));
    }

    internal static string EntryDisplayName(Competition competition, EntryId entryId)
    {
        var entry = competition.Entries.FirstOrDefault(item => item.Id.Equals(entryId));
        return entry?.DisplayName ?? string.Empty;
    }
}

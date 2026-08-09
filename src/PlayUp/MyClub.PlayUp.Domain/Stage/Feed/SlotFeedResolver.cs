// -----------------------------------------------------------------------
// <copyright file="SlotFeedResolver.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Pure WhoFeeds helper: resolves configuration feeds from a snapshot (no aggregate loading).
/// </summary>
public static class SlotFeedResolver
{
    /// <summary>
    /// Resolves all slots in the snapshot.
    /// </summary>
    /// <param name="snapshot">Immutable feed snapshot.</param>
    /// <returns>One resolution per slot key, in snapshot order.</returns>
    public static IReadOnlyList<SlotFeedResolution> ResolveAll(SlotFeedSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return [..snapshot.SlotKeys.Select(key => Resolve(snapshot, key))];
    }

    /// <summary>
    /// Resolves a single slot key.
    /// </summary>
    /// <param name="snapshot">Immutable feed snapshot.</param>
    /// <param name="slotKey">Slot key to resolve.</param>
    /// <returns>The resolution for that slot.</returns>
    public static SlotFeedResolution Resolve(SlotFeedSnapshot snapshot, string slotKey)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var key = Slot.NormalizeKey(slotKey);
        if (!snapshot.SlotKeys.Contains(key, StringComparer.Ordinal))
        {
            throw new DomainException(
                $"Slot '{key}' is not part of the feed snapshot.",
                StageErrorCodes.FeedSnapshotInvalid);
        }

        var directs = snapshot.DirectAssignments
            .Where(d => string.Equals(d.SlotKey, key, StringComparison.Ordinal))
            .ToArray();
        var qualifications = snapshot.InboundQualification
            .Where(q => string.Equals(q.DestinationSlotKey, key, StringComparison.Ordinal))
            .ToArray();
        var progressions = snapshot.InboundProgression
            .Where(p => string.Equals(p.DestinationSlotKey, key, StringComparison.Ordinal))
            .ToArray();
        var draws = snapshot.DrawTargets
            .Where(d => string.Equals(d.SlotKey, key, StringComparison.Ordinal))
            .ToArray();

        var counts = new Dictionary<FeedKind, int>
        {
            [FeedKind.Direct] = directs.Length,
            [FeedKind.Qualification] = qualifications.Length,
            [FeedKind.Progression] = progressions.Length,
            [FeedKind.Draw] = draws.Length
        };

        var contributing = counts
            .Where(pair => pair.Value > 0)
            .Select(pair => pair.Key)
            .Order()
            .ToArray();

        if (contributing.Length == 0)
        {
            return SlotFeedResolution.Missing(key);
        }

        // InvalidFeed takes precedence when any kind has multiple sources.
        if (counts.Values.Any(count => count > 1))
        {
            return SlotFeedResolution.Invalid(key, contributing);
        }

        if (contributing.Length > 1)
        {
            return SlotFeedResolution.Multiple(key, contributing);
        }

        var kind = contributing[0];
        var source = kind switch
        {
            FeedKind.Direct => UniqueFeedSource.ForDirect(directs[0].ConfiguredEntryId),
            FeedKind.Qualification => UniqueFeedSource.ForQualification(
                qualifications[0].SourceStageId,
                qualifications[0].PathOrder),
            FeedKind.Progression => UniqueFeedSource.ForProgression(
                progressions[0].SourceStageId,
                progressions[0].SourceFixtureId,
                progressions[0].Outcome),
            FeedKind.Draw => UniqueFeedSource.ForDraw(),
            _ => throw new DomainException(
                "Unknown feed kind.",
                StageErrorCodes.FeedSnapshotInvalid)
        };

        return SlotFeedResolution.Unique(key, source);
    }
}

// -----------------------------------------------------------------------
// <copyright file="SlotFeedResolution.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Result of resolving configuration feeds for one slot (not persisted).
/// </summary>
public sealed record SlotFeedResolution
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlotFeedResolution"/> class.
    /// </summary>
    public SlotFeedResolution(
        string slotKey,
        FeedResolutionStatus status,
        UniqueFeedSource? source,
        IReadOnlyList<FeedKind> contributingKinds)
    {
        ArgumentNullException.ThrowIfNull(contributingKinds);

        if (!Enum.IsDefined(status))
        {
            throw new DomainException(
                "Feed resolution status is unknown.",
                StageErrorCodes.FeedSnapshotInvalid);
        }

        var isUnique = status == FeedResolutionStatus.Unique;
        if (isUnique != source is not null)
        {
            throw new DomainException(
                "Unique status requires a source, and a source is only allowed when status is Unique.",
                StageErrorCodes.FeedSnapshotInvalid);
        }

        SlotKey = Slot.NormalizeKey(slotKey);
        Status = status;
        Source = source;
        ContributingKinds = [..contributingKinds];
    }

    /// <summary>
    /// Gets the slot key.
    /// </summary>
    public string SlotKey { get; }

    /// <summary>
    /// Gets the resolution status.
    /// </summary>
    public FeedResolutionStatus Status { get; }

    /// <summary>
    /// Gets the unique source when <see cref="Status"/> is <see cref="FeedResolutionStatus.Unique"/>.
    /// </summary>
    public UniqueFeedSource? Source { get; }

    /// <summary>
    /// Gets feed kinds that contributed at least one source.
    /// </summary>
    public IReadOnlyList<FeedKind> ContributingKinds { get; }

    /// <summary>
    /// Creates a missing resolution.
    /// </summary>
    public static SlotFeedResolution Missing(string slotKey) =>
        new(slotKey, FeedResolutionStatus.Missing, null, []);

    /// <summary>
    /// Creates a unique resolution.
    /// </summary>
    public static SlotFeedResolution Unique(string slotKey, UniqueFeedSource source) =>
        new(slotKey, FeedResolutionStatus.Unique, source, [source.Kind]);

    /// <summary>
    /// Creates a multiple-feeds resolution.
    /// </summary>
    public static SlotFeedResolution Multiple(string slotKey, IReadOnlyList<FeedKind> contributingKinds) =>
        new(slotKey, FeedResolutionStatus.MultipleFeeds, null, contributingKinds);

    /// <summary>
    /// Creates an invalid-feed resolution.
    /// </summary>
    public static SlotFeedResolution Invalid(string slotKey, IReadOnlyList<FeedKind> contributingKinds) =>
        new(slotKey, FeedResolutionStatus.InvalidFeed, null, contributingKinds);
}

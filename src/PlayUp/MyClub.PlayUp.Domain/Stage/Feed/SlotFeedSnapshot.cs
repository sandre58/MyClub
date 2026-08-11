// -----------------------------------------------------------------------
// <copyright file="SlotFeedSnapshot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Immutable configuration snapshot used by <see cref="SlotFeedResolver"/> (no <see cref="Slot.EntryId"/>).
/// </summary>
public sealed record SlotFeedSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlotFeedSnapshot"/> class.
    /// </summary>
    public SlotFeedSnapshot(
        StageId targetStageId,
        IReadOnlyList<string> slotKeys,
        IReadOnlyList<DirectFeedSource> directAssignments,
        IReadOnlyList<QualificationFeedSource> inboundQualification,
        IReadOnlyList<ProgressionFeedSource> inboundProgression,
        IReadOnlyList<DrawFeedSource> drawTargets)
    {
        ArgumentNullException.ThrowIfNull(slotKeys);
        ArgumentNullException.ThrowIfNull(directAssignments);
        ArgumentNullException.ThrowIfNull(inboundQualification);
        ArgumentNullException.ThrowIfNull(inboundProgression);
        ArgumentNullException.ThrowIfNull(drawTargets);

        var normalizedKeys = slotKeys.Select(Slot.NormalizeKey).ToArray();
        if (normalizedKeys.Distinct(StringComparer.Ordinal).Count() != normalizedKeys.Length)
        {
            throw new DomainException(
                "Slot keys in a feed snapshot must be unique.",
                StageErrorCodes.FeedSnapshotInvalid);
        }

        var keySet = normalizedKeys.ToHashSet(StringComparer.Ordinal);

        foreach (var direct in directAssignments)
        {
            if (!keySet.Contains(direct.SlotKey))
            {
                throw new DomainException(
                    $"Direct feed targets unknown slot '{direct.SlotKey}'.",
                    StageErrorCodes.FeedSnapshotInvalid);
            }
        }

        foreach (var qualification in inboundQualification)
        {
            if (!keySet.Contains(qualification.DestinationSlotKey))
            {
                throw new DomainException(
                    $"Qualification feed targets unknown slot '{qualification.DestinationSlotKey}'.",
                    StageErrorCodes.FeedSnapshotInvalid);
            }
        }

        foreach (var progression in inboundProgression)
        {
            if (!keySet.Contains(progression.DestinationSlotKey))
            {
                throw new DomainException(
                    $"Progression feed targets unknown slot '{progression.DestinationSlotKey}'.",
                    StageErrorCodes.FeedSnapshotInvalid);
            }
        }

        foreach (var draw in drawTargets)
        {
            if (!keySet.Contains(draw.SlotKey))
            {
                throw new DomainException(
                    $"Draw feed targets unknown slot '{draw.SlotKey}'.",
                    StageErrorCodes.FeedSnapshotInvalid);
            }
        }

        TargetStageId = targetStageId;
        SlotKeys = normalizedKeys;
        DirectAssignments = [..directAssignments];
        InboundQualification = [..inboundQualification];
        InboundProgression = [..inboundProgression];
        DrawTargets = [..drawTargets];
    }

    /// <summary>
    /// Gets the target stage identity.
    /// </summary>
    public StageId TargetStageId { get; }

    /// <summary>
    /// Gets the slot keys to resolve.
    /// </summary>
    public IReadOnlyList<string> SlotKeys { get; }

    /// <summary>
    /// Gets direct assignment feeds.
    /// </summary>
    public IReadOnlyList<DirectFeedSource> DirectAssignments { get; }

    /// <summary>
    /// Gets inbound qualification feeds.
    /// </summary>
    public IReadOnlyList<QualificationFeedSource> InboundQualification { get; }

    /// <summary>
    /// Gets inbound progression feeds (self and cross-stage).
    /// </summary>
    public IReadOnlyList<ProgressionFeedSource> InboundProgression { get; }

    /// <summary>
    /// Gets draw feed targets (Published Slot resolutions contributing to WhoFeeds).
    /// </summary>
    public IReadOnlyList<DrawFeedSource> DrawTargets { get; }
}

// -----------------------------------------------------------------------
// <copyright file="QualificationFeedSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Snapshot entry for an inbound qualification path targeting a slot.
/// </summary>
public sealed record QualificationFeedSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationFeedSource"/> class.
    /// </summary>
    /// <param name="sourceStageId">Stage that owns the qualification rules.</param>
    /// <param name="pathOrder"><see cref="Rules.QualificationPath.Order"/>.</param>
    /// <param name="destinationSlotKey">Target slot key on the destination stage.</param>
    public QualificationFeedSource(StageId sourceStageId, int pathOrder, string destinationSlotKey)
    {
        if (pathOrder < 1)
        {
            throw new DomainException(
                "Qualification path order must be at least 1.",
                StageErrorCodes.FeedSnapshotInvalid);
        }

        SourceStageId = sourceStageId;
        PathOrder = pathOrder;
        DestinationSlotKey = Slot.NormalizeKey(destinationSlotKey);
    }

    /// <summary>
    /// Gets the source stage identity.
    /// </summary>
    public StageId SourceStageId { get; }

    /// <summary>
    /// Gets the qualification path order.
    /// </summary>
    public int PathOrder { get; }

    /// <summary>
    /// Gets the destination slot key.
    /// </summary>
    public string DestinationSlotKey { get; }
}

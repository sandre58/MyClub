// -----------------------------------------------------------------------
// <copyright file="ProgressionFeedSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Snapshot entry for an inbound progression path targeting a slot.
/// </summary>
public sealed record ProgressionFeedSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionFeedSource"/> class.
    /// </summary>
    /// <param name="sourceStageId">Stage that owns the progression rules.</param>
    /// <param name="sourcePairKey">Structural source key (Cup = PairKey).</param>
    /// <param name="outcome">Winner or loser.</param>
    /// <param name="destinationSlotKey">Target slot key on the destination stage.</param>
    public ProgressionFeedSource(
        StageId sourceStageId,
        string sourcePairKey,
        ProgressionOutcome outcome,
        string destinationSlotKey)
    {
        if (!Enum.IsDefined(outcome))
        {
            throw new DomainException(
                "Progression outcome is unknown.",
                StageErrorCodes.FeedSnapshotInvalid);
        }

        SourceStageId = sourceStageId;
        SourcePairKey = BracketPair.NormalizePairKey(sourcePairKey);
        Outcome = outcome;
        DestinationSlotKey = Slot.NormalizeKey(destinationSlotKey);
    }

    /// <summary>
    /// Gets the source stage identity.
    /// </summary>
    public StageId SourceStageId { get; }

    /// <summary>
    /// Gets the structural source confrontation key.
    /// </summary>
    public string SourcePairKey { get; }

    /// <summary>
    /// Gets the confrontation outcome.
    /// </summary>
    public ProgressionOutcome Outcome { get; }

    /// <summary>
    /// Gets the destination slot key.
    /// </summary>
    public string DestinationSlotKey { get; }
}

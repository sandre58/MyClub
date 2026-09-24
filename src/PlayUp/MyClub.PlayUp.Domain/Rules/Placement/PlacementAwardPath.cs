// -----------------------------------------------------------------------
// <copyright file="PlacementAwardPath.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Declarative award of a final competition rank from a structural confrontation outcome.
/// Value object — no technical identity and no Order.
/// Distinct from <see cref="ProgressionPath"/> (routing to a slot).
/// Cup V1 source identity = <see cref="BracketPair.PairKey"/> (not FixtureId).
/// </summary>
public sealed record PlacementAwardPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlacementAwardPath"/> class.
    /// </summary>
    /// <param name="sourcePairKey">
    /// Structural source key — Cup V1 = <see cref="BracketPair.PairKey"/>.
    /// </param>
    /// <param name="outcome">Winner or loser of the confrontation.</param>
    /// <param name="rank">1-based final competition rank awarded to that participant.</param>
    public PlacementAwardPath(string sourcePairKey, ProgressionOutcome outcome, int rank)
    {
        if (!Enum.IsDefined(outcome))
        {
            throw new DomainException(
                "Placement award outcome is unknown.",
                RulesErrorCodes.PlacementAwardRulesInvalid);
        }

        if (rank < 1)
        {
            throw new DomainException(
                "Placement award rank must be at least 1.",
                RulesErrorCodes.PlacementAwardRulesInvalid);
        }

        SourcePairKey = BracketPair.NormalizePairKey(sourcePairKey);
        Outcome = outcome;
        Rank = rank;
    }

    /// <summary>
    /// Gets the structural source confrontation key (Cup = PairKey).
    /// </summary>
    public string SourcePairKey { get; }

    /// <summary>
    /// Gets the confrontation outcome selector (Winner or Loser).
    /// </summary>
    public ProgressionOutcome Outcome { get; }

    /// <summary>
    /// Gets the 1-based final competition rank awarded.
    /// </summary>
    public int Rank { get; }

    /// <summary>
    /// Returns an independent copy.
    /// </summary>
    /// <returns>A copy of this path.</returns>
    public PlacementAwardPath Copy() => new(SourcePairKey, Outcome, Rank);
}

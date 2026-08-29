// -----------------------------------------------------------------------
// <copyright file="PlacementAwardPath.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Declarative award of a final competition rank from a Fixture/Tie outcome.
/// Value object — no technical identity and no Order.
/// Distinct from <see cref="ProgressionPath"/> (routing to a slot).
/// </summary>
public sealed record PlacementAwardPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlacementAwardPath"/> class.
    /// </summary>
    /// <param name="sourceFixtureId">Fixture identity owned by the stage that carries the placement award rules.</param>
    /// <param name="outcome">Winner or loser of the confrontation.</param>
    /// <param name="rank">1-based final competition rank awarded to that participant.</param>
    public PlacementAwardPath(FixtureId sourceFixtureId, ProgressionOutcome outcome, int rank)
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

        SourceFixtureId = sourceFixtureId;
        Outcome = outcome;
        Rank = rank;
    }

    /// <summary>
    /// Gets the source fixture identity.
    /// </summary>
    public FixtureId SourceFixtureId { get; }

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
    public PlacementAwardPath Copy() => new(SourceFixtureId, Outcome, Rank);
}

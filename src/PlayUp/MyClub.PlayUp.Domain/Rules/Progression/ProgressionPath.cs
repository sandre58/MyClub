// -----------------------------------------------------------------------
// <copyright file="ProgressionPath.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Declarative routing from a structural confrontation outcome to a destination (population or slot).
/// Value object — no technical identity and no Order (unlike <see cref="QualificationPath"/>).
/// Cup source identity = <see cref="BracketPair.PairKey"/> (not FixtureId).
/// </summary>
public sealed record ProgressionPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionPath"/> class.
    /// </summary>
    /// <param name="sourcePairKey">
    /// Structural source key — Cup = <see cref="BracketPair.PairKey"/>.
    /// </param>
    /// <param name="outcome">Winner or loser of the confrontation.</param>
    /// <param name="destination">Where the selected participant is routed.</param>
    public ProgressionPath(
        string sourcePairKey,
        ProgressionOutcome outcome,
        ProgressionDestination destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!Enum.IsDefined(outcome))
        {
            throw new DomainException(
                "Progression outcome is unknown.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        SourcePairKey = BracketPair.NormalizePairKey(sourcePairKey);
        Outcome = outcome;
        Destination = destination;
    }

    /// <summary>
    /// Gets the structural source confrontation key (Cup = PairKey).
    /// </summary>
    public string SourcePairKey { get; }

    /// <summary>
    /// Gets the confrontation outcome.
    /// </summary>
    public ProgressionOutcome Outcome { get; }

    /// <summary>
    /// Gets the destination.
    /// </summary>
    public ProgressionDestination Destination { get; }

    /// <summary>
    /// Returns an independent copy (new nested value-object instances).
    /// </summary>
    public ProgressionPath Copy() => new(SourcePairKey, Outcome, Destination.Copy());
}

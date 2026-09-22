// -----------------------------------------------------------------------
// <copyright file="ProgressionPath.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Declarative routing from a Fixture/Tie outcome to a destination (population or slot).
/// Value object — no technical identity and no Order (unlike <see cref="QualificationPath"/>).
/// Does not embed TieFormat resolution.
/// </summary>
public sealed record ProgressionPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionPath"/> class.
    /// </summary>
    /// <param name="sourceFixtureId">Fixture identity owned by the stage that carries the progression rules.</param>
    /// <param name="outcome">Winner or loser of the confrontation.</param>
    /// <param name="destination">Where the selected participant is routed.</param>
    public ProgressionPath(
        FixtureId sourceFixtureId,
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

        SourceFixtureId = sourceFixtureId;
        Outcome = outcome;
        Destination = destination;
    }

    /// <summary>
    /// Gets the source fixture identity.
    /// </summary>
    public FixtureId SourceFixtureId { get; }

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
    /// <returns>A deep copy of this path.</returns>
    public ProgressionPath Copy()
    {
        return new ProgressionPath(SourceFixtureId, Outcome, Destination.Copy());
    }
}

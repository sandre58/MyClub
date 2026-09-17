// -----------------------------------------------------------------------
// <copyright file="ProgressionIntent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Persisted authoring unit: one round × outcome intention that expands to N <see cref="ProgressionPath"/>.
/// Paths are derived (Expand) — Intent is the authoring source of truth (Qual/Prog V3).
/// Destination is peer Population or Place on the form-owning (source) stage.
/// </summary>
public sealed record ProgressionIntent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionIntent"/> class.
    /// </summary>
    public ProgressionIntent(
        IntentId id,
        int order,
        RoundId roundId,
        ProgressionOutcome outcome,
        ProgressionDestination destination)
    {
        if (order < 1)
        {
            throw new DomainException(
                "Progression intent order must be at least 1.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        if (!Enum.IsDefined(outcome))
        {
            throw new DomainException(
                "Progression intent outcome is unknown.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        ArgumentNullException.ThrowIfNull(destination);

        Id = id;
        Order = order;
        RoundId = roundId;
        Outcome = outcome;
        Destination = destination;
    }

    /// <summary>Gets the stable authoring identity (Guid v7).</summary>
    public IntentId Id { get; }

    /// <summary>Gets the display / processing order among intents (≥ 1).</summary>
    public int Order { get; }

    /// <summary>Gets the source round whose fixtures expand to paths.</summary>
    public RoundId RoundId { get; }

    /// <summary>Gets Winner or Loser applied to each fixture of the round.</summary>
    public ProgressionOutcome Outcome { get; }

    /// <summary>
    /// Gets the destination effect: Population (peer) or Place (forme-owner).
    /// Same destination is applied to every expanded path.
    /// </summary>
    public ProgressionDestination Destination { get; }

    /// <summary>Returns a deep copy.</summary>
    public ProgressionIntent Copy() =>
        new(Id, Order, RoundId, Outcome, Destination.Copy());
}

// -----------------------------------------------------------------------
// <copyright file="PenaltyShootout.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.MatchAggregate.MatchEvents;

/// <summary>
/// Represents a penalty shootout attempt during a football match.
/// Penalty shootouts are used to determine the winner of matches that end in a draw
/// and require a decisive result, typically in knockout competitions.
/// </summary>
public class PenaltyShootout : Entity<PenaltyShootoutId>
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private PenaltyShootout() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PenaltyShootout"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the penalty shootout attempt.</param>
    /// <param name="takerId">The identifier of the player who took the penalty. Can be null if not specified.</param>
    /// <param name="result">The outcome of the penalty attempt (succeeded, failed, or not yet determined).</param>
    private PenaltyShootout(PenaltyShootoutId id, PlayerId? takerId = null, PenaltyShootoutOutcome result = PenaltyShootoutOutcome.None)
        : base(id)
    {
        TakerId = takerId;
        Result = result;
    }

    /// <summary>
    /// Creates a new penalty shootout attempt with the specified parameters.
    /// </summary>
    /// <param name="takerId">The identifier of the player taking the penalty. Optional.</param>
    /// <param name="result">The outcome of the penalty attempt. Defaults to None if not specified.</param>
    /// <returns>A new <see cref="PenaltyShootout"/> instance.</returns>
    public static PenaltyShootout Create(PlayerId? takerId = null, PenaltyShootoutOutcome result = PenaltyShootoutOutcome.None) => new(PenaltyShootoutId.New(), takerId, result);

    /// <summary>
    /// Gets the identifier of the match to which this penalty shootout belongs.
    /// This property is used internally by Entity Framework Core to maintain the relationship.
    /// </summary>
    [UsedImplicitly(Reason = "Used by EF Core.")]
    internal MatchId MatchId { get; private set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the player who took this penalty.
    /// Can be null if the penalty taker is not specified or not yet determined.
    /// </summary>
    public PlayerId? TakerId { get; set; }

    /// <summary>
    /// Gets or sets the outcome of this penalty attempt.
    /// </summary>
    public PenaltyShootoutOutcome Result { get; set; }

    /// <summary>
    /// Returns a string representation of the penalty shootout attempt.
    /// </summary>
    /// <returns>A formatted string showing the result and optionally the penalty taker.</returns>
    public override string ToString()
    {
        var str = new StringBuilder();

        _ = Result switch
        {
            PenaltyShootoutOutcome.Succeeded => str.Append('O'),
            PenaltyShootoutOutcome.Failed => str.Append('X'),
            PenaltyShootoutOutcome.None => str.Append('?'),
            _ => str.Append(string.Empty)
        };
        if (TakerId is not null)
            _ = str.Append(CultureInfo.CurrentCulture, $" ({TakerId})");

        return str.ToString();
    }
}

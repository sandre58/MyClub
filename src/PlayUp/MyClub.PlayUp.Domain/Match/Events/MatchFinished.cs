// -----------------------------------------------------------------------
// <copyright file="MatchFinished.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Match.Events;

/// <summary>
/// Raised when a match is finished with a result.
/// </summary>
public sealed record MatchFinished : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchFinished"/> class.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <param name="resultType">How the result was obtained.</param>
    /// <param name="score">The final score.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public MatchFinished(MatchId matchId, ResultType resultType, Score score, IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        ResultType = resultType;
        Score = score;
    }

    /// <summary>
    /// Gets the match identity.
    /// </summary>
    public MatchId MatchId { get; }

    /// <summary>
    /// Gets how the result was obtained.
    /// </summary>
    public ResultType ResultType { get; }

    /// <summary>
    /// Gets the final score.
    /// </summary>
    public Score Score { get; }
}

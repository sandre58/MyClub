// -----------------------------------------------------------------------
// <copyright file="MatchResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Match;

/// <summary>
/// Outcome of a finished match: result type and score.
/// </summary>
public sealed record MatchResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchResult"/> class.
    /// </summary>
    /// <param name="type">How the result was obtained.</param>
    /// <param name="score">The final score.</param>
    public MatchResult(ResultType type, Score score)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                $"Unknown result type '{type}'.",
                MatchErrorCodes.InvalidResult);
        }

        Type = type;
        Score = score;
    }

    /// <summary>
    /// Gets how the result was obtained.
    /// </summary>
    public ResultType Type { get; }

    /// <summary>
    /// Gets the final score.
    /// </summary>
    public Score Score { get; }
}

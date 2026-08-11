// -----------------------------------------------------------------------
// <copyright file="MatchResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Match;

/// <summary>
/// Sporting outcome of a finished match: result type, play score, optional extra time fact, optional shootout.
/// </summary>
/// <remarks>
/// <para>
/// Structural invariants (local to this VO; Match does not load MatchRules / TieFormat):
/// </para>
/// <list type="number">
/// <item><description><see cref="PenaltyShootoutScore"/> may exist only when <see cref="Score"/> is equal (Home == Away).</description></item>
/// <item><description>When shootout is present, shootout Home ≠ Away.</description></item>
/// <item><description>Unequal play score ⇒ shootout must be null.</description></item>
/// <item><description><see cref="ExtraTimePlayed"/> is orthogonal (AET without shootout, or shootout without ET).</description></item>
/// </list>
/// <para>
/// <see cref="ExtraTimePlayed"/> does not participate in fixture outcome resolution.
/// </para>
/// </remarks>
public sealed record MatchResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchResult"/> class.
    /// </summary>
    /// <param name="type">How the result was obtained.</param>
    /// <param name="score">Final play score (extra time goals included when played; shootout excluded).</param>
    /// <param name="extraTimePlayed">Whether extra time was actually played.</param>
    /// <param name="penaltyShootoutScore">Shootout score when taken; otherwise <see langword="null"/>.</param>
    public MatchResult(
        ResultType type,
        Score score,
        bool extraTimePlayed = false,
        PenaltyShootoutScore? penaltyShootoutScore = null)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                $"Unknown result type '{type}'.",
                MatchErrorCodes.InvalidResult);
        }

        if (penaltyShootoutScore is { } shootout)
        {
            if (score.HomeGoals != score.AwayGoals)
            {
                throw new DomainException(
                    "Penalty shootout requires an equal play score.",
                    MatchErrorCodes.InvalidResult);
            }

            if (shootout.HomeGoals == shootout.AwayGoals)
            {
                throw new DomainException(
                    "Penalty shootout score must be decisive (home ≠ away).",
                    MatchErrorCodes.InvalidResult);
            }
        }

        Type = type;
        Score = score;
        ExtraTimePlayed = extraTimePlayed;
        PenaltyShootoutScore = penaltyShootoutScore;
    }

    /// <summary>
    /// Gets how the result was obtained.
    /// </summary>
    public ResultType Type { get; }

    /// <summary>
    /// Gets the final play score (extra time included when played; shootout excluded).
    /// </summary>
    public Score Score { get; }

    /// <summary>
    /// Gets a value indicating whether extra time was actually played.
    /// </summary>
    public bool ExtraTimePlayed { get; }

    /// <summary>
    /// Gets the penalty shootout score when taken; otherwise <see langword="null"/>.
    /// </summary>
    public PenaltyShootoutScore? PenaltyShootoutScore { get; }
}

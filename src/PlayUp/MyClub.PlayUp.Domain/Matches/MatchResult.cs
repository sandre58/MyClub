// -----------------------------------------------------------------------
// <copyright file="MatchResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Sporting outcome of a finished match: result type, play score, optional extra time fact, optional shootout.
/// </summary>
/// <remarks>
/// <para>
/// Structural invariants (local to this VO; Match does not load MatchRules / TieFormat):
/// </para>
/// <list type="number">
/// <item><description><see cref="PenaltyShootoutScore"/> may exist only when <see cref="Score"/> is equal (Home == Away), unless <see cref="ConfrontationTiePenaltyShootout"/> is set.</description></item>
/// <item><description>When shootout is present, shootout Home ≠ Away.</description></item>
/// <item><description>Unequal play score ⇒ shootout must be null unless <see cref="ConfrontationTiePenaltyShootout"/> is set.</description></item>
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
        ValidateCommon(type, score, penaltyShootoutScore);
        if (penaltyShootoutScore is { } shootout && score.HomeGoals != score.AwayGoals)
        {
            throw new DomainException(
                "Penalty shootout requires an equal play score.",
                MatchErrorCodes.InvalidResult);
        }

        Type = type;
        Score = score;
        ExtraTimePlayed = extraTimePlayed;
        PenaltyShootoutScore = penaltyShootoutScore;
        ConfrontationTiePenaltyShootout = false;
    }

    private MatchResult(
        ResultType type,
        Score score,
        bool extraTimePlayed,
        PenaltyShootoutScore shootout,
        bool confrontationTiePenaltyShootout)
    {
        ValidateCommon(type, score, shootout);
        if (!confrontationTiePenaltyShootout)
        {
            throw new InvalidOperationException("Use the public constructor when the confrontation tie flag is false.");
        }

        if (type != ResultType.Played)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout is allowed only for played results.",
                MatchErrorCodes.InvalidResult);
        }

        if (score.HomeGoals == score.AwayGoals)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires an unequal play score on the final leg.",
                MatchErrorCodes.InvalidResult);
        }

        Type = type;
        Score = score;
        ExtraTimePlayed = extraTimePlayed;
        PenaltyShootoutScore = shootout;
        ConfrontationTiePenaltyShootout = true;
    }

    /// <summary>
    /// Creates a played result with <see cref="ConfrontationTiePenaltyShootout"/> after route eligibility and a domain proof.
    /// </summary>
    /// <param name="score">Unequal final-leg play score (shootout excluded).</param>
    /// <param name="extraTimePlayed">Whether extra time was played.</param>
    /// <param name="penaltyShootoutScore">Decisive shootout score.</param>
    /// <param name="proof">Proof that the aggregate was level when the result was entered.</param>
    public static MatchResult CreateConfrontationTiePenaltyShootout(
        Score score,
        bool extraTimePlayed,
        PenaltyShootoutScore penaltyShootoutScore,
        ConfrontationTieEligibilityProof proof)
    {
        ArgumentNullException.ThrowIfNull(proof);

        return new(ResultType.Played, score, extraTimePlayed, penaltyShootoutScore, confrontationTiePenaltyShootout: true);
    }

    /// <summary>
    /// Rehydrates a persisted <see cref="MatchResult"/> without re-running route eligibility.
    /// </summary>
    /// <remarks>
    /// When <paramref name="confrontationTiePenaltyShootout"/> is true, the flag is taken as stored on disk:
    /// eligibility is not replayed (sister leg and live rules are not loaded). The fixture outcome resolver
    /// remains the authority for the qualified entry. Only shape invariants are enforced here.
    /// </remarks>
    /// <param name="type">Stored result type.</param>
    /// <param name="score">Stored play score.</param>
    /// <param name="extraTimePlayed">Stored extra time flag.</param>
    /// <param name="penaltyShootoutScore">Stored shootout when present.</param>
    /// <param name="confrontationTiePenaltyShootout">Stored confrontation tie flag.</param>
    /// <returns>The rehydrated result.</returns>
    public static MatchResult Rehydrate(
        ResultType type,
        Score score,
        bool extraTimePlayed,
        PenaltyShootoutScore? penaltyShootoutScore,
        bool confrontationTiePenaltyShootout = false)
    {
        if (!confrontationTiePenaltyShootout)
        {
            return new MatchResult(type, score, extraTimePlayed, penaltyShootoutScore);
        }

        if (type != ResultType.Played)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout is allowed only for played results.",
                MatchErrorCodes.InvalidResult);
        }

        if (penaltyShootoutScore is null)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a decisive shootout score.",
                MatchErrorCodes.InvalidResult);
        }

        return new MatchResult(type, score, extraTimePlayed, penaltyShootoutScore.Value, confrontationTiePenaltyShootout: true);
    }

    private static void ValidateCommon(ResultType type, Score score, PenaltyShootoutScore? penaltyShootoutScore)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                $"Unknown result type '{type}'.",
                MatchErrorCodes.InvalidResult);
        }

        if (penaltyShootoutScore is { } shootout && shootout.HomeGoals == shootout.AwayGoals)
        {
            throw new DomainException(
                "Penalty shootout score must be decisive (home ≠ away).",
                MatchErrorCodes.InvalidResult);
        }
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

    /// <summary>
    /// Gets a value indicating whether the shootout on an unequal final-leg score was entered
    /// because the two-leg aggregate (after away goals when active) is still level.
    /// </summary>
    public bool ConfrontationTiePenaltyShootout { get; }
}

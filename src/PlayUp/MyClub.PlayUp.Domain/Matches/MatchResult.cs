// -----------------------------------------------------------------------
// <copyright file="MatchResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

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
    /// Single domain entry point for a played confrontation-tie shootout on an unequal final-leg score.
    /// </summary>
    /// <param name="tieFormat">Effective confrontation format (aggregate, away goals, penalty rule).</param>
    /// <param name="firstLegHomeEntryId">First-leg home entry.</param>
    /// <param name="firstLegAwayEntryId">First-leg away entry.</param>
    /// <param name="firstLegStatus">First-leg match status.</param>
    /// <param name="firstLegScore">First-leg play score.</param>
    /// <param name="firstLegExtraTimePlayed">Whether extra time was played on the first leg.</param>
    /// <param name="returnLegHomeEntryId">Return-leg home entry.</param>
    /// <param name="returnLegAwayEntryId">Return-leg away entry.</param>
    /// <param name="returnLegScore">Return-leg play score (shootout excluded).</param>
    /// <param name="returnLegExtraTimePlayed">Whether extra time was played on the return leg.</param>
    /// <param name="penaltyShootoutScore">Decisive shootout on the return leg.</param>
    /// <param name="firstLegMatchId">Optional first-leg match identity for snapshots.</param>
    /// <param name="returnLegMatchId">Optional return-leg match identity for snapshots.</param>
    public static MatchResult CreateConfrontationTiePenaltyShootout(
        TieFormat tieFormat,
        EntryId firstLegHomeEntryId,
        EntryId firstLegAwayEntryId,
        MatchStatus firstLegStatus,
        Score firstLegScore,
        bool firstLegExtraTimePlayed,
        EntryId returnLegHomeEntryId,
        EntryId returnLegAwayEntryId,
        Score returnLegScore,
        bool returnLegExtraTimePlayed,
        PenaltyShootoutScore penaltyShootoutScore,
        MatchId? firstLegMatchId = null,
        MatchId? returnLegMatchId = null)
    {
        ArgumentNullException.ThrowIfNull(tieFormat);

        if (tieFormat.NumberOfLegs != TieFormat.TwoLegs || !tieFormat.AggregateScoring)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a two-leg aggregate format.",
                MatchErrorCodes.InvalidResult);
        }

        if (tieFormat.PenaltyShootoutRule is null)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a penalty shootout rule.",
                MatchErrorCodes.InvalidResult);
        }

        if (firstLegStatus != MatchStatus.Finished)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a finished first leg with a score.",
                MatchErrorCodes.InvalidResult);
        }

        if (returnLegScore.HomeGoals == returnLegScore.AwayGoals)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires an unequal play score on the final leg.",
                MatchErrorCodes.InvalidResult);
        }

        if (penaltyShootoutScore.HomeGoals == penaltyShootoutScore.AwayGoals)
        {
            throw new DomainException(
                "Penalty shootout score must be decisive (home ≠ away).",
                MatchErrorCodes.InvalidResult);
        }

        if (!returnLegHomeEntryId.Equals(firstLegAwayEntryId) || !returnLegAwayEntryId.Equals(firstLegHomeEntryId))
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires home and away entries to swap on the return leg.",
                MatchErrorCodes.InvalidResult);
        }

        var firstLegId = firstLegMatchId ?? MatchId.New();
        var returnLegId = returnLegMatchId ?? MatchId.New();
        var legs = new FixtureLegSnapshot[]
        {
            new(
                1,
                firstLegId,
                firstLegHomeEntryId,
                firstLegAwayEntryId,
                firstLegStatus,
                firstLegScore,
                firstLegExtraTimePlayed,
                penaltyShootoutScore: null),
            new(
                2,
                returnLegId,
                returnLegHomeEntryId,
                returnLegAwayEntryId,
                MatchStatus.Finished,
                returnLegScore,
                returnLegExtraTimePlayed,
                penaltyShootoutScore),
        };

        var (entryA, entryB) = ResolveEntryPair(legs);
        if (!TwoLegAggregateStillLevel.AfterPlayScores(tieFormat, entryA, entryB, legs))
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a level aggregate after play scores.",
                MatchErrorCodes.InvalidResult);
        }

        return new MatchResult(
            ResultType.Played,
            returnLegScore,
            returnLegExtraTimePlayed,
            penaltyShootoutScore,
            confrontationTiePenaltyShootout: true);
    }

    /// <summary>
    /// Rehydrates a persisted <see cref="MatchResult"/> without re-running route eligibility.
    /// </summary>
    /// <remarks>
    /// When <paramref name="confrontationTiePenaltyShootout"/> is true, the flag is taken as stored on disk:
    /// eligibility is not replayed (sister leg and live rules are not loaded). The fixture outcome resolver
    /// remains the authority for the qualified entry. Only shape invariants are enforced here.
    /// </remarks>
    internal static MatchResult Rehydrate(
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

    private static (EntryId EntryA, EntryId EntryB) ResolveEntryPair(IReadOnlyList<FixtureLegSnapshot> legs)
    {
        var entries = legs
            .SelectMany(l => new[] { l.HomeEntryId, l.AwayEntryId })
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        if (entries.Length != 2)
        {
            throw new DomainException(
                "Fixture confrontation requires exactly two distinct entries across all legs.",
                MatchErrorCodes.InvalidResult);
        }

        return (entries[0], entries[1]);
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

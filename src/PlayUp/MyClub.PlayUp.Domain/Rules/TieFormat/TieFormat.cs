// -----------------------------------------------------------------------
// <copyright file="TieFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// How to determine the winner of a knockout confrontation (legs, aggregate, away goals, ET/TAB of the tie).
/// Distinct from <see cref="MatchRules"/> (how a single match is played).
/// </summary>
public sealed record TieFormat
{
    /// <summary>
    /// Single-leg confrontation.
    /// </summary>
    public const int SingleLeg = 1;

    /// <summary>
    /// Two-legged confrontation (home and away).
    /// </summary>
    public const int TwoLegs = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="TieFormat"/> class.
    /// </summary>
    /// <param name="numberOfLegs">Number of legs (1 or 2).</param>
    /// <param name="aggregateScoring">Whether leg scores are aggregated.</param>
    /// <param name="awayGoalsRule">Away-goals rule when enabled; <see langword="null"/> when disabled.</param>
    /// <param name="extraTimeRule">Extra time to resolve the tie when enabled; <see langword="null"/> when disabled.</param>
    /// <param name="penaltyShootoutRule">Penalty shootout to resolve the tie when enabled; <see langword="null"/> when disabled.</param>
    public TieFormat(
        int numberOfLegs,
        bool aggregateScoring,
        AwayGoalsRule? awayGoalsRule = null,
        ExtraTimeRule? extraTimeRule = null,
        PenaltyShootoutRule? penaltyShootoutRule = null)
    {
        if (numberOfLegs is not (SingleLeg or TwoLegs))
        {
            throw new DomainException(
                "Number of legs must be 1 or 2 in V1.",
                RulesErrorCodes.TieFormatInvalid);
        }

        switch (numberOfLegs)
        {
            case SingleLeg when aggregateScoring:
                throw new DomainException(
                    "Aggregate scoring requires two legs.",
                    RulesErrorCodes.TieFormatInvalid);
            case TwoLegs when !aggregateScoring:
                throw new DomainException(
                    "Two-legged ties require aggregate scoring in V1.",
                    RulesErrorCodes.TieFormatInvalid);
        }

        if (awayGoalsRule is not null && (numberOfLegs != TwoLegs || !aggregateScoring))
        {
            throw new DomainException(
                "Away goals rule requires two legs with aggregate scoring.",
                RulesErrorCodes.TieFormatInvalid);
        }

        NumberOfLegs = numberOfLegs;
        AggregateScoring = aggregateScoring;
        AwayGoalsRule = awayGoalsRule;
        ExtraTimeRule = extraTimeRule;
        PenaltyShootoutRule = penaltyShootoutRule;
    }

    /// <summary>
    /// Gets the number of legs (1 or 2).
    /// </summary>
    public int NumberOfLegs { get; }

    /// <summary>
    /// Gets a value indicating whether leg scores are aggregated.
    /// </summary>
    public bool AggregateScoring { get; }

    /// <summary>
    /// Gets the away-goals rule when enabled; otherwise <see langword="null"/>.
    /// </summary>
    public AwayGoalsRule? AwayGoalsRule { get; }

    /// <summary>
    /// Gets the extra-time rule for resolving the tie when enabled; otherwise <see langword="null"/>.
    /// </summary>
    public ExtraTimeRule? ExtraTimeRule { get; }

    /// <summary>
    /// Gets the penalty shootout rule for resolving the tie when enabled; otherwise <see langword="null"/>.
    /// </summary>
    public PenaltyShootoutRule? PenaltyShootoutRule { get; }

    /// <summary>
    /// Returns an independent copy (new nested marker instances when present).
    /// </summary>
    /// <returns>A deep copy of this tie format.</returns>
    public TieFormat Copy() =>
        new(
            NumberOfLegs,
            AggregateScoring,
            AwayGoalsRule is null ? null : new AwayGoalsRule(),
            ExtraTimeRule is null ? null : new ExtraTimeRule(),
            PenaltyShootoutRule is null ? null : new PenaltyShootoutRule());
}

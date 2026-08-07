// -----------------------------------------------------------------------
// <copyright file="StandingRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Standing calculation: points barème and ordered ranking criteria.
/// </summary>
public sealed record StandingRules
{
    private readonly RankingCriterion[] _rankingCriteria;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandingRules"/> class.
    /// </summary>
    /// <param name="points">Points awarded for win / draw / loss.</param>
    /// <param name="rankingCriteria">Ordered ranking criteria (non-empty, no duplicates, V1 values only).</param>
    public StandingRules(PointsPolicy points, IReadOnlyList<RankingCriterion> rankingCriteria)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(rankingCriteria);

        if (rankingCriteria.Count == 0)
        {
            throw new DomainException(
                "Ranking criteria cannot be empty.",
                RulesErrorCodes.RankingCriteriaInvalid);
        }

        if (rankingCriteria.Any(c => !Enum.IsDefined(c)))
        {
            throw new DomainException(
                "Ranking criteria contain an unknown value.",
                RulesErrorCodes.RankingCriteriaInvalid);
        }

        if (rankingCriteria.Distinct().Count() != rankingCriteria.Count)
        {
            throw new DomainException(
                "Ranking criteria cannot contain duplicates.",
                RulesErrorCodes.RankingCriteriaInvalid);
        }

        Points = points;
        _rankingCriteria = [..rankingCriteria];
    }

    /// <summary>
    /// Gets the points policy.
    /// </summary>
    public PointsPolicy Points { get; }

    /// <summary>
    /// Gets the ordered ranking criteria.
    /// </summary>
    public IReadOnlyList<RankingCriterion> RankingCriteria => _rankingCriteria;

    /// <inheritdoc />
    public bool Equals(StandingRules? other) =>
        other is not null
        && Points.Equals(other.Points)
        && _rankingCriteria.SequenceEqual(other._rankingCriteria);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _rankingCriteria.Aggregate(Points.GetHashCode(), HashCode.Combine);
}

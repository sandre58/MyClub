// -----------------------------------------------------------------------
// <copyright file="QualificationCondition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Optional gate evaluated on a selected standing row after selection (Points ≥ threshold).
/// </summary>
public sealed record QualificationCondition
{
    private QualificationCondition(int minimumPoints)
    {
        if (minimumPoints < 0)
        {
            throw new DomainException(
                "Qualification condition minimum points cannot be negative.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        MinimumPoints = minimumPoints;
    }

    /// <summary>
    /// Gets the inclusive minimum points required on the selected standing row.
    /// </summary>
    public int MinimumPoints { get; }

    /// <summary>
    /// Creates a condition requiring <see cref="StandingRow.Points"/> ≥ <paramref name="minimumPoints"/>.
    /// </summary>
    /// <param name="minimumPoints">Inclusive threshold (≥ 0).</param>
    /// <returns>A points condition.</returns>
    public static QualificationCondition PointsAtLeast(int minimumPoints) =>
        new(minimumPoints);

    /// <summary>
    /// Evaluates this condition against a standing row.
    /// </summary>
    /// <param name="row">Selected standing row.</param>
    /// <returns><see langword="true"/> when the row satisfies the condition.</returns>
    public bool IsSatisfiedBy(StandingRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return row.Points >= MinimumPoints;
    }
}

// -----------------------------------------------------------------------
// <copyright file="QualificationSelection.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Describes which participants are selected from a ranking (mode + value). No ranking computation.
/// </summary>
public sealed record QualificationSelection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationSelection"/> class.
    /// </summary>
    /// <param name="mode">How participants are selected.</param>
    /// <param name="value">
    /// Position, count, or range lower bound depending on <paramref name="mode"/> (≥ 1).
    /// </param>
    /// <param name="endValue">
    /// Inclusive upper bound when <paramref name="mode"/> is <see cref="SelectionMode.Range"/>; otherwise <see langword="null"/>.
    /// </param>
    public QualificationSelection(SelectionMode mode, int value, int? endValue = null)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new DomainException(
                "Selection mode is unknown.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (value < 1)
        {
            throw new DomainException(
                "Selection value must be at least 1.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (mode == SelectionMode.Range)
        {
            if (endValue is null)
            {
                throw new DomainException(
                    "Range selection requires an end value.",
                    RulesErrorCodes.QualificationRulesInvalid);
            }

            if (endValue.Value < value)
            {
                throw new DomainException(
                    "Range end value must be greater than or equal to the start value.",
                    RulesErrorCodes.QualificationRulesInvalid);
            }
        }
        else if (endValue is not null)
        {
            throw new DomainException(
                "End value is only allowed for Range selection.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        Mode = mode;
        Value = value;
        EndValue = endValue;
    }

    /// <summary>
    /// Gets the selection mode.
    /// </summary>
    public SelectionMode Mode { get; }

    /// <summary>
    /// Gets the position, count, or range lower bound associated with the mode.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Gets the inclusive range upper bound when <see cref="Mode"/> is <see cref="SelectionMode.Range"/>; otherwise <see langword="null"/>.
    /// </summary>
    public int? EndValue { get; }
}

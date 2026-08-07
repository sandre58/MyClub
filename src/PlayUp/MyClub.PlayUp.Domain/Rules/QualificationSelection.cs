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
    /// <param name="value">Position or count depending on <paramref name="mode"/> (&gt; 0).</param>
    public QualificationSelection(SelectionMode mode, int value)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new DomainException(
                "Selection mode is unknown.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (value <= 0)
        {
            throw new DomainException(
                "Selection value must be greater than 0.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        Mode = mode;
        Value = value;
    }

    /// <summary>
    /// Gets the selection mode.
    /// </summary>
    public SelectionMode Mode { get; }

    /// <summary>
    /// Gets the position or count associated with the mode.
    /// </summary>
    public int Value { get; }
}

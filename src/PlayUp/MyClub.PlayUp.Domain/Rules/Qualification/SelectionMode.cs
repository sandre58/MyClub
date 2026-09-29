// -----------------------------------------------------------------------
// <copyright file="SelectionMode.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// How participants are selected from a ranking source for a qualification path.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// A single ranking position (e.g. position 1).
    /// </summary>
    Position = 0,

    /// <summary>
    /// The top N ranked participants.
    /// </summary>
    Top = 1,

    /// <summary>
    /// The bottom N ranked participants.
    /// </summary>
    Bottom = 2,

    /// <summary>
    /// Inclusive ranking range from <see cref="QualificationSelection.Value"/> to <see cref="QualificationSelection.EndValue"/>.
    /// </summary>
    Range = 5
}

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
    /// The best N participants across the ranking scope (representable; resolution deferred).
    /// </summary>
    Best = 3,

    /// <summary>
    /// The worst N participants across the ranking scope (representable; resolution deferred).
    /// </summary>
    Worst = 4,

    /// <summary>
    /// Inclusive ranking range from <see cref="QualificationSelection.Value"/> to <see cref="QualificationSelection.EndValue"/>.
    /// </summary>
    Range = 5
}

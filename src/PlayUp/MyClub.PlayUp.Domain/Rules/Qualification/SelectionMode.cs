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
    /// Obsolete alias of <see cref="Top"/> (JSON int 3). Normalized to <see cref="Top"/> on construction.
    /// </summary>
    [Obsolete("Use Top. Persisted/JSON value 3 is normalized to Top.")]
    Best = 3,

    /// <summary>
    /// Obsolete alias of <see cref="Bottom"/> (JSON int 4). Normalized to <see cref="Bottom"/> on construction.
    /// </summary>
    [Obsolete("Use Bottom. Persisted/JSON value 4 is normalized to Bottom.")]
    Worst = 4,

    /// <summary>
    /// Inclusive ranking range from <see cref="QualificationSelection.Value"/> to <see cref="QualificationSelection.EndValue"/>.
    /// </summary>
    Range = 5
}

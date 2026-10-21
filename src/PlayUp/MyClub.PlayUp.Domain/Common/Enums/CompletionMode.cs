// -----------------------------------------------------------------------
// <copyright file="CompletionMode.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// How a competition was completed (manner of ending, not a causal status).
/// </summary>
public enum CompletionMode
{
    /// <summary>
    /// Competition completed through its normal sporting process.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Organizer closed the competition with an official outcome.
    /// </summary>
    Administrative = 1,

    /// <summary>
    /// Competition was stopped without a sporting conclusion.
    /// </summary>
    Abandoned = 2
}

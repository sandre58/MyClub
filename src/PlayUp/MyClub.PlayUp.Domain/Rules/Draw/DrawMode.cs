// -----------------------------------------------------------------------
// <copyright file="DrawMode.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Execution mode for attempting a draw resolution (parameters only; not the definition of Draw).
/// Draw is the business procedure that selects among admissible resolutions; this enum only
/// describes how an automated attempt may be produced. Manual ceremony is a future extension.
/// </summary>
public enum DrawMode
{
    /// <summary>
    /// Automated attempt may use randomness (subject to constraints).
    /// Does not imply that every Draw resolution comes from a random generator —
    /// recorded or assisted resolutions remain valid.
    /// </summary>
    Random = 0
}

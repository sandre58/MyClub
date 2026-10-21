// -----------------------------------------------------------------------
// <copyright file="StructureDrawExecutionBadge.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Lightweight topology badge for draw execution (not DrawRules config, not full lifecycle enums).
/// </summary>
/// <remarks>
/// Visible only when DrawRules are engaged. Maps active Draw (+ Applied) to four product states.
/// Detail (Draft / Published / Resolution) stays in the Executions dialog.
/// </remarks>
public enum StructureDrawExecutionBadge
{
    /// <summary>DrawRules present; no useful non-cancelled execution.</summary>
    ToLaunch = 0,

    /// <summary>Active Draft execution (resolved or not / NoSolution).</summary>
    InProgress = 1,

    /// <summary>Published and not yet applied.</summary>
    ToApply = 2,

    /// <summary>Published and applied.</summary>
    Applied = 3
}

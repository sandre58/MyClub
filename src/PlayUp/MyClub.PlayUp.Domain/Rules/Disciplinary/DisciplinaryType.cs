// -----------------------------------------------------------------------
// <copyright file="DisciplinaryType.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Closed V1 catalogue of disciplinary event types known to MyClub.
/// A competition may only authorize types from this catalogue (<see cref="DisciplinaryRules.AllowedTypes"/>).
/// Types do not encode consequences (presence, tempo, suspensions, …).
/// </summary>
public enum DisciplinaryType
{
    /// <summary>
    /// Yellow card (or equivalent caution). No Domain consequence in V1.
    /// </summary>
    Yellow = 0,

    /// <summary>
    /// Red card (or equivalent sending-off signal). No Domain consequence in V1.
    /// </summary>
    Red = 1,

    /// <summary>
    /// White card (competition-specific meaning). No Domain consequence in V1.
    /// </summary>
    White = 2
}

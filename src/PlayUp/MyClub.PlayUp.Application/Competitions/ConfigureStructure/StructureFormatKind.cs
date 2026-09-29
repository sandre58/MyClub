// -----------------------------------------------------------------------
// <copyright file="StructureFormatKind.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application-only structure format intents (not a Domain aggregate).
/// </summary>
public enum StructureFormatKind
{
    /// <summary>Championship: matchdays skeleton.</summary>
    Championship = 0,

    /// <summary>Group phase: empty groups + matchdays + pot draw rules.</summary>
    Groups = 1,

    /// <summary>Cup / knockout: round + slots (bracket size power of two).</summary>
    Cup = 2,

    /// <summary>Swiss: SwissSettings + progressive Matchdays via GenerateNextRound.</summary>
    Swiss = 3
}

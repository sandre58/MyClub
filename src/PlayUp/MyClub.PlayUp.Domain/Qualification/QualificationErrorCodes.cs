// -----------------------------------------------------------------------
// <copyright file="QualificationErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Qualification;

/// <summary>
/// Stable machine-readable codes for Qualification application invariant violations.
/// </summary>
public static class QualificationErrorCodes
{
    /// <summary>
    /// Gets the code when the selection mode is unknown to the applier.
    /// </summary>
    public const string SelectionNotSupported = "Qualification.SelectionNotSupported";

    /// <summary>
    /// Gets the code when a path selects more than one entry (V1: one path → one slot).
    /// </summary>
    public const string PathMultiEntry = "Qualification.PathMultiEntry";

    /// <summary>
    /// Gets the code when the selection cannot be resolved from the standing.
    /// </summary>
    public const string SelectionUnresolved = "Qualification.SelectionUnresolved";
}

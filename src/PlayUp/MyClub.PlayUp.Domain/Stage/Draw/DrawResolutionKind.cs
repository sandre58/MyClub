// -----------------------------------------------------------------------
// <copyright file="DrawResolutionKind.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Principal result kind produced by a Draw (V1: one kind per Draw).
/// </summary>
public enum DrawResolutionKind
{
    /// <summary>
    /// Entry → Slot placements.
    /// </summary>
    Slot = 0,

    /// <summary>
    /// Entry → Group placements.
    /// </summary>
    Group = 1,

    /// <summary>
    /// Entry ↔ Entry pairing proposals (does not create Match).
    /// </summary>
    Pairing = 2
}

// -----------------------------------------------------------------------
// <copyright file="DrawResolutionKind.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Principal result kind produced by a Draw (one kind per Draw).
/// Cup uses Slot only; Swiss pairing is a separate Domain concern.
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
    Group = 1
}

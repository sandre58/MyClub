// -----------------------------------------------------------------------
// <copyright file="FinalPlacementInstruction.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Placement;

/// <summary>
/// Deterministic final-rank instruction from a placement award (no Stage mutation).
/// </summary>
/// <param name="Rank">1-based final competition rank.</param>
/// <param name="EntryId">Entry that receives the rank.</param>
public sealed record FinalPlacementInstruction(int Rank, EntryId EntryId);

// -----------------------------------------------------------------------
// <copyright file="ApplyDrawResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Result of <see cref="ApplyDraw.Execute"/>.
/// </summary>
/// <param name="SlotInstructions">Slot instructions when kind is Slot; otherwise empty.</param>
/// <param name="CreatedMatches">Always empty (Slot/Group apply does not create matches).</param>
public sealed record ApplyDrawResult(
    IReadOnlyList<SlotAssignmentInstruction> SlotInstructions,
    IReadOnlyList<Match> CreatedMatches);

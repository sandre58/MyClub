// -----------------------------------------------------------------------
// <copyright file="AddStageSlotRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for adding a slot to a stage.
/// </summary>
/// <param name="SlotKey">Opaque positional slot key.</param>
public sealed record AddStageSlotRequest(string SlotKey);

/// <summary>
/// HTTP response after creating a slot.
/// </summary>
/// <param name="SlotKey">Normalized slot key.</param>
public sealed record AddStageSlotResponse(string SlotKey);

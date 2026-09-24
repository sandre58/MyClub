// -----------------------------------------------------------------------
// <copyright file="AssignEntryToSlotRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for assigning an entry to a slot (Placement manuel Coupe).
/// </summary>
/// <param name="EntryId">Competition entry to pin on the slot.</param>
public sealed record AssignEntryToSlotRequest(Guid EntryId);

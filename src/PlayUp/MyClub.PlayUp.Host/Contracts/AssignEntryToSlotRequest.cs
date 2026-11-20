// -----------------------------------------------------------------------
// <copyright file="AssignEntryToSlotRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for assigning an entry to a slot (manual Cup placement).
/// </summary>
/// <param name="EntryId">Competition entry to pin on the slot.</param>
/// <param name="ExpectedEntryId">
/// Resolved occupant the caller observed. Required in the JSON body.
/// <see langword="null"/> means the slot was free. A missing property is not that value.
/// </param>
public sealed record AssignEntryToSlotRequest(
    Guid EntryId,
    [property: JsonRequired] Guid? ExpectedEntryId);

// -----------------------------------------------------------------------
// <copyright file="AssignEntryToGroupRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// Assign a competition entry to a group (manual placement).
/// </summary>
/// <param name="EntryId">Entry identity.</param>
public sealed record AssignEntryToGroupRequest(Guid EntryId);

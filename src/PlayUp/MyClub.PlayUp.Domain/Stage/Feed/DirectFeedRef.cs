// -----------------------------------------------------------------------
// <copyright file="DirectFeedRef.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Unique direct feed identity (<see cref="DirectAssignment.EntryId"/>, never <see cref="Slot.EntryId"/>).
/// </summary>
public sealed record DirectFeedRef(EntryId ConfiguredEntryId);

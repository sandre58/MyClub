// -----------------------------------------------------------------------
// <copyright file="ReplaceEntryInGroupRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing the team on a group seat.
/// </summary>
/// <param name="ReplacementEntryId">Entry that takes the occupant's seat.</param>
public sealed record ReplaceEntryInGroupRequest(Guid ReplacementEntryId);

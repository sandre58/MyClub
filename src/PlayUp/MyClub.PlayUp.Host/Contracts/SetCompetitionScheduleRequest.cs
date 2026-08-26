// -----------------------------------------------------------------------
// <copyright file="SetCompetitionScheduleRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for setting declared competition schedule (both null clears).
/// </summary>
/// <param name="ScheduledStart">Declared start.</param>
/// <param name="ScheduledEnd">Declared end.</param>
public sealed record SetCompetitionScheduleRequest(DateTimeOffset? ScheduledStart, DateTimeOffset? ScheduledEnd);

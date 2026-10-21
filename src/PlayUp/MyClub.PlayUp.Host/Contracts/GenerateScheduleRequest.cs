// -----------------------------------------------------------------------
// <copyright file="GenerateScheduleRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for generating a schedule proposal.
/// </summary>
public sealed record GenerateScheduleRequest(
    DateTimeOffset HorizonStart,
    DateTimeOffset HorizonEnd,
    int GranularityMinutes,
    string TimeZoneId,
    IReadOnlyList<Guid> ResourceIds,
    IReadOnlyList<Guid>? TargetMatchIds = null,
    int MatchDurationMinutes = 90);

// -----------------------------------------------------------------------
// <copyright file="RenameStageRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>HTTP body for renaming a stage.</summary>
/// <param name="Name">New display name.</param>
public sealed record RenameStageRequest(string Name);

/// <summary>HTTP body for adding a matchday.</summary>
/// <param name="Number">Optional 1-based number; next available when omitted.</param>
public sealed record AddStageMatchdayRequest(int? Number = null);

/// <summary>HTTP response after adding a matchday.</summary>
public sealed record AddStageMatchdayResponse(Guid MatchdayId, int Number);

/// <summary>HTTP body for adding a group.</summary>
/// <param name="Name">Optional group name; A/B/… when omitted.</param>
public sealed record AddStageGroupRequest(string? Name = null);

/// <summary>HTTP response after adding a group.</summary>
public sealed record AddStageGroupResponse(Guid GroupId, string Name);

/// <summary>HTTP body for match generation format.</summary>
/// <param name="Format">SingleRoundRobin | DoubleRoundRobin.</param>
public sealed record ReplaceStageMatchGenerationFormatRequest(string Format);

/// <summary>HTTP body for Swiss planned rounds.</summary>
/// <param name="RoundCount">Planned Swiss rounds K (≥ 1).</param>
public sealed record ReplaceStageSwissSettingsRequest(int RoundCount);

// -----------------------------------------------------------------------
// <copyright file="ApplyDrawRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for applying a published draw (Slot/Group). FixtureIds are ignored (legacy clients may still send []).
/// </summary>
/// <param name="FixtureIds">Ignored. Kept for JSON compat with older clients.</param>
public sealed record ApplyDrawRequest(IReadOnlyList<Guid>? FixtureIds = null);

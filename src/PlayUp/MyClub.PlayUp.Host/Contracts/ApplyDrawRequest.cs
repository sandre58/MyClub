// -----------------------------------------------------------------------
// <copyright file="ApplyDrawRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for applying a published draw. Pairing may omit fixtures (auto-provisioned).
/// </summary>
/// <param name="FixtureIds">Optional target fixture identities (order matches PairingResults).</param>
public sealed record ApplyDrawRequest(IReadOnlyList<Guid>? FixtureIds = null);

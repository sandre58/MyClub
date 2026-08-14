// -----------------------------------------------------------------------
// <copyright file="ApplyDrawRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for applying a published draw. Pairing requires one fixture id per pairing result.
/// </summary>
/// <param name="FixtureIds">Target fixture identities (order matches PairingResults).</param>
public sealed record ApplyDrawRequest(IReadOnlyList<Guid> FixtureIds);

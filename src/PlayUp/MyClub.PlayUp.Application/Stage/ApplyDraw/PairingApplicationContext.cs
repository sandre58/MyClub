// -----------------------------------------------------------------------
// <copyright file="PairingApplicationContext.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Stage;

/// <summary>
/// Application orchestration input for Pairing draw apply (not Draw domain data).
/// </summary>
/// <param name="FixtureId">Existing fixture that receives all matches from the pairing resolution.</param>
public sealed record PairingApplicationContext(FixtureId FixtureId);

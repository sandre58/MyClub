// -----------------------------------------------------------------------
// <copyright file="PairingApplicationContext.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application orchestration input for Pairing draw apply (not Draw domain data).
/// </summary>
/// <param name="FixtureIds">One existing fixture per pairing result, same order as DrawResolution.PairingResults.</param>
public sealed record PairingApplicationContext(IReadOnlyList<FixtureId> FixtureIds)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PairingApplicationContext"/> class for a single pairing → single fixture.
    /// </summary>
    public PairingApplicationContext(FixtureId fixtureId)
        : this([fixtureId])
    {
    }
}

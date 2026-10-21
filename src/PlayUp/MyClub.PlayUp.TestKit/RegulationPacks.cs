// -----------------------------------------------------------------------
// <copyright file="RegulationPacks.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.TestKit;

/// <summary>
/// Shared regulation packs for tests. Delegates to Application bootstrap to avoid a second baseline.
/// </summary>
public static class RegulationPacks
{
    /// <summary>
    /// Standard amateur football baseline (same numbers as <see cref="BootstrapRegulation.Standard"/>).
    /// </summary>
    /// <returns>A new <see cref="Regulation"/> instance.</returns>
    public static Regulation Standard() => BootstrapRegulation.Standard();
}

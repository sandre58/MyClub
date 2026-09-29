// -----------------------------------------------------------------------
// <copyright file="SampleRegulations.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.TestKit;

namespace MyClub.PlayUp.Application.Tests.Common;

/// <summary>
/// Compatibility shim — prefer <see cref="RegulationPacks"/> in new tests.
/// </summary>
internal static class SampleRegulations
{
    public static Regulation Standard() => RegulationPacks.Standard();
}

// -----------------------------------------------------------------------
// <copyright file="ReplaceRegulation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: replace competition regulation as a whole.
/// </summary>
/// <remarks>
/// Distinct from <see cref="BootstrapRegulation"/> (Create default only). Ready demotes to Draft via Domain.
/// </remarks>
public static class ReplaceRegulation
{
    /// <summary>
    /// Replaces the competition regulation.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="regulation">New regulation (copied by Domain).</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Competition competition, Regulation regulation, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(regulation);
        ArgumentNullException.ThrowIfNull(clock);
        competition.ReplaceRegulation(regulation, clock);
    }
}

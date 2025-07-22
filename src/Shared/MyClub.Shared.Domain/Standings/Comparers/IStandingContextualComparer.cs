// -----------------------------------------------------------------------
// <copyright file="IStandingContextualComparer.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Standings.Rules;

namespace MyClub.Shared.Domain.Standings.Comparers;

public interface IStandingContextualComparer : IStandingComparer
{
    /// <summary>
    /// Sets the context for the comparer by specifying the matches and rules to be used.
    /// </summary>
    /// <remarks>This method allows the injection of specific matches and rules into the comparer, enabling
    /// customized comparison logic. Ensure that both <paramref name="matches"/> and <paramref name="rules"/> are
    /// provided to avoid unexpected behavior.</remarks>
    /// <param name="matches">A collection of matches that the comparer will evaluate. Cannot be null.</param>
    /// <param name="rules">The set of rules that define how standings are calculated. Cannot be null.</param>
    void SetContext(IEnumerable<IMatch> matches, StandingRuleSet rules);
}

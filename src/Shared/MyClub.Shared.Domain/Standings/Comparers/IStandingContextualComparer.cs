// -----------------------------------------------------------------------
// <copyright file="IStandingContextualComparer.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Standings.Rules;

namespace MyClub.Shared.Domain.Standings.Comparers;

/// <summary>
/// Interface for standing comparers that require contextual information to perform comparisons.
/// Extends IStandingComparer to provide additional context about matches and rules.
/// This is particularly useful for complex comparisons like head-to-head records that need access to match data.
/// </summary>
public interface IStandingContextualComparer : IStandingComparer
{
    /// <summary>
    /// Sets the context for the comparer by specifying the matches and rules to be used.
    /// This method allows the injection of specific matches and rules into the comparer, enabling
    /// customized comparison logic based on the actual match results and standing rules.
    /// </summary>
    /// <param name="matches">A collection of matches that the comparer will evaluate. Cannot be null.</param>
    /// <param name="rules">The set of rules that define how standings are calculated. Cannot be null.</param>
    /// <remarks>
    /// Ensure that both parameters are provided to avoid unexpected behavior.
    /// This method should be called before performing any comparisons to ensure accurate results.
    /// </remarks>
    void SetContext(IEnumerable<IMatch> matches, StandingRuleSet rules);
}

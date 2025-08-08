// -----------------------------------------------------------------------
// <copyright file="IKnockout.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Scorer.Domain.RoundAggregate;

namespace MyClub.Scorer.Domain.Primitives;

/// <summary>
/// Defines the contract for knockout-style competitions that feature elimination rounds.
/// This interface extends <see cref="ITeamsContainer"/> to provide knockout-specific functionality
/// for competitions where teams are eliminated through successive rounds until only one winner remains.
/// </summary>
public interface IKnockout : ITeamsContainer
{
    /// <summary>
    /// Gets the read-only collection of round identifiers that belong to this knockout competition.
    /// Rounds represent the elimination phases where teams compete to advance to the next stage.
    /// </summary>
    /// <value>
    /// A read-only collection of <see cref="RoundId"/> representing the elimination rounds.
    /// Rounds are typically organized in a hierarchical structure (e.g., Round of 16, Quarter-finals, Semi-finals, Final).
    /// </value>
    /// <remarks>
    /// The rounds collection defines the tournament bracket structure. Each round typically contains
    /// fixtures between teams, with winners advancing to subsequent rounds and losers being eliminated.
    /// The number and structure of rounds depends on the number of participating teams and the specific
    /// knockout format being used.
    /// </remarks>
    IReadOnlyCollection<RoundId> Rounds { get; }
}

// -----------------------------------------------------------------------
// <copyright file="Regulation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Competition regulation value object: entry, match, and standing rules.
/// Immutable; replace as a whole on change.
/// </summary>
public sealed record Regulation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Regulation"/> class.
    /// </summary>
    /// <param name="entryRules">Registration constraints.</param>
    /// <param name="matchRules">How an individual match is played.</param>
    /// <param name="standingRules">How standings are calculated.</param>
    public Regulation(EntryRules entryRules, MatchRules matchRules, StandingRules standingRules)
    {
        ArgumentNullException.ThrowIfNull(entryRules);
        ArgumentNullException.ThrowIfNull(matchRules);
        ArgumentNullException.ThrowIfNull(standingRules);

        EntryRules = entryRules;
        MatchRules = matchRules;
        StandingRules = standingRules;
    }

    /// <summary>
    /// Gets the entry rules.
    /// </summary>
    public EntryRules EntryRules { get; }

    /// <summary>
    /// Gets the match rules.
    /// </summary>
    public MatchRules MatchRules { get; }

    /// <summary>
    /// Gets the standing rules.
    /// </summary>
    public StandingRules StandingRules { get; }
}

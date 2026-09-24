// -----------------------------------------------------------------------
// <copyright file="ProgressionChampionshipPath.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Resolves the championship-path terminal round for Winner Sorties.
/// Business meaning: Winner = championship exit of the form — not
/// <c>max(round list order)</c> (Finale + Match 3ᵉ must not confuse that).
/// </summary>
/// <remarks>
/// Mono-round form (Cup V1): the single round is terminal without fixtures.
/// Multi-round interim: longest classic KO prefix among rounds that have fixtures
/// (halving fixture counts). Remaining rounds (e.g. 3ᵉ place after Finale) excluded.
/// </remarks>
public static class ProgressionChampionshipPath
{
    /// <summary>
    /// Returns the championship-path terminal round, or <see langword="null"/>
    /// when the form cannot resolve a terminal.
    /// </summary>
    /// <param name="rounds">Stage rounds in Structure / storage order.</param>
    public static Round? TerminalRound(IReadOnlyList<Round> rounds)
    {
        ArgumentNullException.ThrowIfNull(rounds);

        switch (rounds.Count)
        {
            case 0:
                return null;

            // Structural mono-round (Cup V1) — no fixtures required.
            case 1:
                return rounds[0];
        }

        var withFixtures = rounds.Where(static r => r.Fixtures.Count > 0).ToList();
        switch (withFixtures.Count)
        {
            case 0:
                return null;
            case 1:
                return withFixtures[0];
        }

        var prefix = new List<Round> { withFixtures[0] };
        for (var i = 1; i < withFixtures.Count; i++)
        {
            var previousCount = prefix[^1].Fixtures.Count;
            var currentCount = withFixtures[i].Fixtures.Count;
            if (previousCount > 1 && currentCount == previousCount / 2)
            {
                prefix.Add(withFixtures[i]);
                continue;
            }

            break;
        }

        return prefix[^1];
    }

    /// <summary>
    /// Returns whether <paramref name="roundId"/> is the championship-path terminal.
    /// </summary>
    public static bool IsChampionshipTerminal(
        IReadOnlyList<Round> rounds,
        RoundId roundId)
    {
        var terminal = TerminalRound(rounds);
        return terminal?.Id.Equals(roundId) == true;
    }
}

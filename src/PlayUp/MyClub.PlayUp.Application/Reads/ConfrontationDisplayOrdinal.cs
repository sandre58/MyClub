// -----------------------------------------------------------------------
// <copyright file="ConfrontationDisplayOrdinal.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Resolves the secondary UI repère <c>Match #n</c> within a round (Cup) or matchday (Championship / Groups).
/// </summary>
internal static class ConfrontationDisplayOrdinal
{
    /// <summary>
    /// 1-based ordinal of a <see cref="BracketPair"/> in structural order (PairKey sort), when present on the stage.
    /// </summary>
    public static int? TryGetForPairKey(Stage stage, string pairKey)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentException.ThrowIfNullOrWhiteSpace(pairKey);

        var normalized = BracketPair.NormalizePairKey(pairKey);
        var ordered = GetOrderedBracketPairs(stage);
        for (var i = 0; i < ordered.Count; i++)
        {
            if (string.Equals(ordered[i].PairKey, normalized, StringComparison.Ordinal))
            {
                return i + 1;
            }
        }

        return null;
    }

    /// <summary>
    /// 1-based ordinal of a fixture within its round or matchday container.
    /// </summary>
    public static int? TryGetForFixture(Stage stage, FixtureId fixtureId)
    {
        ArgumentNullException.ThrowIfNull(stage);

        var fixture = FindFixture(stage, fixtureId);
        if (fixture is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(fixture.BracketPairKey))
        {
            return TryGetForPairKey(stage, fixture.BracketPairKey);
        }

        foreach (var round in stage.Rounds)
        {
            var fixtures = round.Fixtures;
            for (var i = 0; i < fixtures.Count; i++)
            {
                if (fixtures[i].Id.Equals(fixtureId))
                {
                    return i + 1;
                }
            }
        }

        foreach (var matchday in stage.Matchdays.OrderBy(matchday => matchday.Number))
        {
            var fixtures = matchday.Fixtures;
            for (var i = 0; i < fixtures.Count; i++)
            {
                if (fixtures[i].Id.Equals(fixtureId))
                {
                    return i + 1;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Fixtures in a knockout round in display order (structural list order, not FixtureId).
    /// </summary>
    public static IReadOnlyList<Fixture> FixturesInRoundDisplayOrder(Round round)
    {
        ArgumentNullException.ThrowIfNull(round);
        return round.Fixtures;
    }

    private static IReadOnlyList<BracketPair> GetOrderedBracketPairs(Stage stage) =>
    [
        .. stage.BracketPairs
            .OrderBy(pair => pair.PairKey, StringComparer.Ordinal)
    ];

    private static Fixture? FindFixture(Stage stage, FixtureId fixtureId)
    {
        foreach (var round in stage.Rounds)
        {
            foreach (var fixture in round.Fixtures)
            {
                if (fixture.Id.Equals(fixtureId))
                {
                    return fixture;
                }
            }
        }

        return stage.Matchdays.SelectMany(matchday => matchday.Fixtures).FirstOrDefault(fixture => fixture.Id.Equals(fixtureId));
    }
}

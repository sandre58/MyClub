// -----------------------------------------------------------------------
// <copyright file="ResolvePlacementAwards.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Placement;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Pure Application resolution: FixtureOutcome + PlacementAwardRules → FinalPlacementInstruction[].
/// No Stage mutation, no persistence, no Host Apply — Read projection only (Lot D / E).
/// </summary>
public static class ResolvePlacementAwards
{
    /// <summary>
    /// Resolves all determinable final placements across competition stages.
    /// Undecided / unmaterialized sources are skipped (partial outcome — missing ranks are not invented).
    /// </summary>
    /// <param name="stages">Competition stages (canonical instances).</param>
    /// <param name="matchesByStage">Matches keyed by owning stage.</param>
    /// <returns>Determined placements ordered by rank; empty when none can be derived.</returns>
    public static IReadOnlyList<FinalPlacementInstruction> Execute(
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(matchesByStage);

        var byRank = new SortedDictionary<int, EntryId>();

        foreach (var stage in stages)
        {
            var rules = stage.Regulation.PlacementAwardRules;
            if (rules is null)
            {
                continue;
            }

            var stageMatches = matchesByStage.TryGetValue(stage.Id, out var list) ? list : [];
            var sourceKeys = rules.Paths
                .Select(path => path.SourcePairKey)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var sourcePairKey in sourceKeys)
            {
                if (!TryResolveSourceAwards(stage, sourcePairKey, stageMatches, rules, out var instructions))
                {
                    continue;
                }

                foreach (var instruction in instructions)
                {
                    // First determined award for a rank wins; Domain rules already uniquify within one ruleset.
                    byRank.TryAdd(instruction.Rank, instruction.EntryId);
                }
            }
        }

        return [.. byRank.Select(pair => new FinalPlacementInstruction(pair.Key, pair.Value))];
    }

    /// <summary>
    /// Resolves awards for a single fixture when the confrontation is decided.
    /// HTTP/fixture trigger resolves to structural SourcePairKey (fail-closed on Cup without BracketPairKey).
    /// </summary>
    public static IReadOnlyList<FinalPlacementInstruction> ExecuteForFixture(
        Stage stage,
        FixtureId fixtureId,
        IReadOnlyList<Match> matches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matches);

        var rules = stage.Regulation.PlacementAwardRules;
        if (rules is null)
        {
            return [];
        }

        Fixture fixture;
        try
        {
            fixture = stage.GetFixture(fixtureId);
        }
        catch (DomainException)
        {
            return [];
        }

        if (!TryResolveSourcePairKey(stage, fixture, out var sourcePairKey))
        {
            return [];
        }

        return TryResolveSourceAwards(stage, sourcePairKey, matches, rules, out var instructions)
            ? instructions
            : [];
    }

    private static bool TryResolveSourceAwards(
        Stage stage,
        string sourcePairKey,
        IReadOnlyList<Match> matches,
        PlacementAwardRules rules,
        out IReadOnlyList<FinalPlacementInstruction> instructions)
    {
        instructions = [];

        var fixture = ResolveFixtureFromSourcePairKey(stage, sourcePairKey);
        if (fixture is null)
        {
            return false;
        }

        var round = stage.Rounds.FirstOrDefault(candidate =>
            candidate.Fixtures.Any(f => f.Id.Equals(fixture.Id)));
        if (round is null)
        {
            return false;
        }

        if (!AllLegsFinished(fixture, matches))
        {
            return false;
        }

        try
        {
            var tieFormat = TieFormat.OrDefaultOneLeg(round.TieFormat);
            if (fixture.Attachments.Count != tieFormat.NumberOfLegs)
            {
                return false;
            }

            var attachedIds = fixture.Attachments.Select(a => a.MatchId).ToHashSet();
            var fixtureMatches = matches.Where(m => attachedIds.Contains(m.Id)).ToArray();
            if (fixtureMatches.Length != fixture.Attachments.Count)
            {
                return false;
            }

            var snapshot = FixtureConfrontationSnapshotAssembler.Assemble(fixture, fixtureMatches);
            var outcome = FixtureOutcomeResolver.Resolve(tieFormat, snapshot);
            instructions = PlacementAwardApplier.ApplyForSource(rules, sourcePairKey, outcome);
            return instructions.Count > 0;
        }
        catch (DomainException)
        {
            return false;
        }
        catch (ApplicationFailureException)
        {
            return false;
        }
    }

    /// <summary>
    /// Requires <see cref="Fixture.BracketPairKey"/> (structural PairKey).
    /// </summary>
    private static bool TryResolveSourcePairKey(Stage stage, Fixture fixture, out string sourcePairKey)
    {
        _ = stage;
        if (!string.IsNullOrWhiteSpace(fixture.BracketPairKey))
        {
            sourcePairKey = BracketPair.NormalizePairKey(fixture.BracketPairKey);
            return true;
        }

        sourcePairKey = string.Empty;
        return false;
    }

    private static Fixture? ResolveFixtureFromSourcePairKey(Stage stage, string sourcePairKey) =>
        stage.FindFixtureByBracketPairKey(sourcePairKey);

    private static bool AllLegsFinished(Fixture fixture, IReadOnlyList<Match> matches)
    {
        if (fixture.Attachments.Count == 0)
        {
            return false;
        }

        var byId = matches.ToDictionary(m => m.Id);
        foreach (var attachment in fixture.Attachments)
        {
            if (!byId.TryGetValue(attachment.MatchId, out var match)
                || match.Status != MatchStatus.Finished
                || match.Result is null)
            {
                return false;
            }
        }

        return true;
    }
}

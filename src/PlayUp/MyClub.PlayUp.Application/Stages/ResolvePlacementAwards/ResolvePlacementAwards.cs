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
    /// Undecided fixtures are skipped (partial outcome — missing ranks are not invented).
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
            var fixtureIds = rules.Paths
                .Select(path => path.SourceFixtureId)
                .Distinct()
                .ToArray();

            foreach (var fixtureId in fixtureIds)
            {
                if (!TryResolveFixtureAwards(stage, fixtureId, stageMatches, rules, out var instructions))
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
    /// </summary>
    public static IReadOnlyList<FinalPlacementInstruction> ExecuteForFixture(
        Stage stage,
        FixtureId fixtureId,
        IReadOnlyList<Match> matches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matches);

        var rules = stage.Regulation.PlacementAwardRules;
        return rules is null
            ? []
            : TryResolveFixtureAwards(stage, fixtureId, matches, rules, out var instructions)
            ? instructions
            : [];
    }

    private static bool TryResolveFixtureAwards(
        Stage stage,
        FixtureId fixtureId,
        IReadOnlyList<Match> matches,
        PlacementAwardRules rules,
        out IReadOnlyList<FinalPlacementInstruction> instructions)
    {
        instructions = [];

        Fixture fixture;
        try
        {
            fixture = stage.GetFixture(fixtureId);
        }
        catch (DomainException)
        {
            return false;
        }

        var round = stage.Rounds.FirstOrDefault(candidate =>
            candidate.Fixtures.Any(f => f.Id.Equals(fixtureId)));
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
            instructions = PlacementAwardApplier.ApplyForFixture(rules, fixtureId, outcome);
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

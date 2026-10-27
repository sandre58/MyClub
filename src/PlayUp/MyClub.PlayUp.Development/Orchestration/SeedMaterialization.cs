// -----------------------------------------------------------------------
// <copyright file="SeedMaterialization.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;

namespace MyClub.PlayUp.Development.Orchestration;

/// <summary>
/// Seeds group / championship / slot match materialization for development scenarios.
/// </summary>
internal static class SeedMaterialization
{
    public static void AssignGroupsRoundRobin(
        Stage stage,
        IReadOnlyList<CompetitionEntry> entries)
    {
        if (stage.Groups.Count == 0)
        {
            throw new InvalidOperationException("Stage has no groups to assign.");
        }

        var ordered = entries.OrderBy(e => e.Id.Value).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var group = stage.Groups[i % stage.Groups.Count];
            stage.AssignEntryToGroup(group.Id, ordered[i].Id);
        }
    }

    public static IReadOnlyList<Match> MaterializeGroupsMatches(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        string matchIdentityScope = "") =>
        MaterializeGroupsMatchesCore(context, competition, stage, maxMatches: null, matchIdentityScope);

    private static List<Match> MaterializeGroupsMatchesCore(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        int? maxMatches,
        string matchIdentityScope)
    {
        var created = new List<Match>();
        var maxRounds = 0;
        var perGroupRounds =
            new List<(int GroupIndex, IReadOnlyList<IReadOnlyList<(EntryId Home, EntryId Away)>> Rounds)>();
        for (var groupIndex = 0; groupIndex < stage.Groups.Count; groupIndex++)
        {
            var group = stage.Groups[groupIndex];
            if (group.EntryIds.Count < 2)
            {
                continue;
            }

            var rounds = MaterializeMatches.BuildRoundRobinRounds(
                [.. group.EntryIds],
                stage.MatchGenerationFormat);
            perGroupRounds.Add((groupIndex, rounds));
            maxRounds = Math.Max(maxRounds, rounds.Count);
        }

        if (perGroupRounds.Count == 0)
        {
            return [];
        }

        var matchKeyPrefix = string.IsNullOrEmpty(matchIdentityScope) ? string.Empty : $"{matchIdentityScope}-";
        EnsureMatchdays(stage, Math.Max(1, maxRounds), context.Clock);
        var matchdays = stage.Matchdays.OrderBy(m => m.Number).ToList();

        foreach (var (groupIndex, rounds) in perGroupRounds)
        {
            var matchOrdinal = 0;
            for (var roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
            {
                if (maxMatches is > 0 && created.Count >= maxMatches)
                {
                    break;
                }

                var matchday = matchdays[roundIndex];
                foreach (var (home, away) in rounds[roundIndex])
                {
                    if (maxMatches is > 0 && created.Count >= maxMatches)
                    {
                        break;
                    }

                    var fixture = stage.AddFixture(matchday.Id, context.Clock);
                    var match = Match.Create(
                        competition.Id,
                        stage.Id,
                        home,
                        away,
                        context.Ids.Match($"{matchKeyPrefix}g-{groupIndex}-m-{matchOrdinal}"),
                        context.Clock);
                    stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, context.Clock);
                    context.Matches.Add(match);
                    created.Add(match);
                    matchOrdinal++;
                }
            }
        }

        MatchEnrichment.ApplyKickoffs(context, competition, stage, created);
        return created;
    }

    /// <summary>
    /// Creates at most <paramref name="maxMatches"/> group fixtures (stable pair order) for recette seeds.
    /// </summary>
    public static IReadOnlyList<Match> MaterializeGroupsMatchesUpTo(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        int maxMatches,
        string matchIdentityScope = "") =>
        maxMatches <= 0
            ? []
            : MaterializeGroupsMatchesCore(context, competition, stage, maxMatches, matchIdentityScope);

    public static IReadOnlyList<Match> MaterializeChampionshipMatches(
        ScenarioContext context,
        Competition competition,
        Stage stage)
    {
        var entries = competition.Entries
            .Where(e => e.Status == EntryStatus.Active)
            .Select(e => e.Id)
            .ToList();
        var rounds = MaterializeMatches.BuildRoundRobinRounds(entries, stage.MatchGenerationFormat);
        EnsureMatchdays(stage, Math.Max(stage.Matchdays.Count, rounds.Count), context.Clock);
        var matchdays = stage.Matchdays.OrderBy(m => m.Number).ToList();
        var created = new List<Match>();
        var matchOrdinal = 0;
        for (var roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
        {
            var matchday = matchdays[roundIndex];
            foreach (var (home, away) in rounds[roundIndex])
            {
                var fixture = stage.AddFixture(matchday.Id, context.Clock);
                var match = Match.Create(
                    competition.Id,
                    stage.Id,
                    home,
                    away,
                    context.Ids.Match($"ch-m-{matchOrdinal}"),
                    context.Clock);
                stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, context.Clock);
                context.Matches.Add(match);
                created.Add(match);
                matchOrdinal++;
            }
        }

        MatchEnrichment.ApplyKickoffs(context, competition, stage, created);
        return created;
    }

    internal static IReadOnlyList<Match> MaterializeForFormat(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        CompetitionRecipe recipe,
        IReadOnlyList<CompetitionEntry> entries)
        => recipe.Format switch
        {
            RecipeFormat.Groups => AssignThenMaterializeGroups(context, competition, stage, entries),
            RecipeFormat.Cup => SeedSlotDraw.ApplyCupSlotDrawAndMaterialize(context, competition, stage, [stage]),
            RecipeFormat.Championship => MaterializeChampionshipMatches(context, competition, stage),
            RecipeFormat.Swiss => throw new InvalidOperationException(
                "Swiss does not use MaterializeForFormat — ApplySwissProgress after PrepareAndStart."),
            _ => throw new InvalidOperationException($"Unsupported recipe format '{recipe.Format}'.")
        };

    internal static IReadOnlyList<Match> AssignThenMaterializeGroups(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        IReadOnlyList<CompetitionEntry> entries)
    {
        AssignGroupsRoundRobin(stage, entries);
        return MaterializeGroupsMatches(context, competition, stage);
    }

    internal static IReadOnlyList<Match> MaterializeFromSlots(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        IReadOnlyList<string>? pairKeys,
        IReadOnlyList<Stage>? competitionStages = null,
        bool domainPrepareForPartialSlotDrawRecette = false)
    {
        if (domainPrepareForPartialSlotDrawRecette)
        {
            if (stage.Status is StageStatus.Draft)
            {
                // Structure Materialize recette: partial slot draw — PrepareStage requires all slot feeds.
                stage.Prepare(context.Clock);
            }
        }
        else
        {
            var stages = competitionStages ?? [stage];
            StageMaterializationTestSupport.EnsureReadyForMaterialize(
                competition,
                stage,
                stages,
                context.Clock);
        }

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            pairKeys,
            [],
            context.Clock);
        foreach (var match in result.CreatedMatches)
        {
            context.Matches.Add(match);
        }

        MatchEnrichment.ApplyKickoffs(context, competition, stage, result.CreatedMatches);
        return result.CreatedMatches;
    }

    internal static void EnsureMatchdays(Stage stage, int requiredCount, IClock clock)
    {
        var nextNumber = stage.Matchdays.Count == 0
            ? 1
            : stage.Matchdays.Max(m => m.Number) + 1;
        while (stage.Matchdays.Count < requiredCount)
        {
            stage.AddMatchday(nextNumber++, clock);
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="ScenarioOrchestration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Development.Generators;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Development.Orchestration;

/// <summary>
/// Internal helpers that orchestrate Domain / Application operations for scenarios (not a fluent builder).
/// </summary>
internal static class ScenarioOrchestration
{
    public static Competition CreateCompetition(
        ScenarioContext context,
        string name,
        Regulation? regulation = null,
        string? shortName = null,
        string? logoPath = null,
        DateTimeOffset? scheduledStart = null,
        DateTimeOffset? scheduledEnd = null)
    {
        var competition = Competition.Create(
            new CompetitionName(name),
            regulation ?? BootstrapRegulation.Standard(),
            context.Ids.Competition(),
            context.Clock);
        if (shortName is not null || logoPath is not null)
        {
            competition.UpdatePresentation(ShortName.Create(shortName), LogoUri.Create(logoPath), context.Clock);
        }

        if (scheduledStart is not null || scheduledEnd is not null)
        {
            competition.SetSchedule(scheduledStart, scheduledEnd, context.Clock);
        }

        context.Competitions.Add(competition);
        return competition;
    }

    private static Competition CreateCompetitionFromRecipe(ScenarioContext context, CompetitionRecipe recipe)
    {
        if (recipe.TeamNames != TeamNameSource.Dataset
            || string.IsNullOrWhiteSpace(recipe.DatasetCompetitionKey))
        {
            return CreateCompetition(context, recipe.DisplayName);
        }

        var dataset = context.Datasets.Get(recipe.DatasetCompetitionKey);
        return CreateCompetition(
            context,
            recipe.DisplayName,
            shortName: dataset.ShortName,
            logoPath: dataset.LogoPath,
            scheduledStart: dataset.ScheduledStart,
            scheduledEnd: dataset.ScheduledEnd);
    }

    public static IReadOnlyList<CompetitionEntry> RegisterTeams(
        ScenarioContext context,
        Competition competition,
        CompetitionRecipe recipe,
        int? countOverride = null)
    {
        var count = countOverride ?? recipe.TeamCount;
        var entries = new List<CompetitionEntry>(count);
        for (var i = 0; i < count; i++)
        {
            context.Clock.Advance(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(i));
            var (displayName, presentation) = TeamNameGenerator.CreatePresentation(
                context.Entropy,
                i,
                recipe.TeamNames,
                recipe.DatasetCompetitionKey,
                context.Datasets);
            var entry = competition.AddEntry(
                context.Ids.Team($"team-{i}"),
                displayName,
                context.Ids.Entry($"entry-{i}"),
                context.Clock,
                presentation);
            entries.Add(entry);
        }

        return entries;
    }

    public static Stage ConfigurePrimaryStage(
        ScenarioContext context,
        Competition competition,
        CompetitionRecipe recipe)
    {
        var intent = CompetitionRecipeValidator.ToStructureIntent(recipe);
        var stage = Stage.Create(
            competition.Id,
            new StageName(intent.StageName),
            competition.Regulation,
            context.Ids.Stage(),
            context.Clock);
        competition.AddStage(stage.Id, context.Clock);
        ConfigureStructure.Execute(competition, stage, intent, context.Clock);
        context.Stages.Add(stage);
        return stage;
    }

    public static void AssignGroupsRoundRobin(
        ScenarioContext context,
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
            stage.AssignEntryToGroup(group.Id, ordered[i].Id, context.Clock);
        }
    }

    public static IReadOnlyList<Match> MaterializeGroupsMatches(
        ScenarioContext context,
        Competition competition,
        Stage stage)
    {
        var created = new List<Match>();
        var maxRounds = 0;
        var perGroupRounds = new List<(int GroupIndex, IReadOnlyList<IReadOnlyList<(EntryId Home, EntryId Away)>> Rounds)>();
        for (var groupIndex = 0; groupIndex < stage.Groups.Count; groupIndex++)
        {
            var group = stage.Groups[groupIndex];
            var rounds = MaterializeMatches.BuildRoundRobinRounds(
                [.. group.EntryIds],
                stage.MatchGenerationFormat);
            perGroupRounds.Add((groupIndex, rounds));
            maxRounds = Math.Max(maxRounds, rounds.Count);
        }

        EnsureMatchdays(stage, Math.Max(1, maxRounds), context.Clock);
        var matchdays = stage.Matchdays.OrderBy(m => m.Number).ToList();

        foreach (var (groupIndex, rounds) in perGroupRounds)
        {
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
                        context.Ids.Match($"g-{groupIndex}-m-{matchOrdinal}"),
                        context.Clock);
                    stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, context.Clock);
                    context.Matches.Add(match);
                    created.Add(match);
                    matchOrdinal++;
                }
            }
        }

        return created;
    }

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

        return created;
    }

    public static IReadOnlyList<Match> ApplyCupPairingDeterministic(
        ScenarioContext context,
        Competition competition,
        Stage stage)
    {
        var entries = competition.Entries
            .Where(e => e.Status == EntryStatus.Active)
            .OrderBy(e => e.Id.Value)
            .Select(e => e.Id)
            .ToList();

        if (entries.Count < 2 || (entries.Count & (entries.Count - 1)) != 0)
        {
            throw new InvalidOperationException("Cup pairing requires a power-of-two entry count.");
        }

        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, context.Ids.Draw(), context.Clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing(entries), context.Clock);

        var pairings = new List<PairingDrawResult>();
        for (var i = 0; i < entries.Count; i += 2)
        {
            pairings.Add(new PairingDrawResult(entries[i], entries[i + 1]));
        }

        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedPairings(pairings),
            context.Clock);
        stage.PublishDraw(draw.Id, context.Clock);

        var round = stage.Rounds[0];
        while (round.Fixtures.Count < pairings.Count)
        {
            stage.AddFixture(round.Id, context.Clock);
        }

        var fixtures = round.Fixtures.Take(pairings.Count).ToList();
        var applyResult = ApplyDraw.Execute(
            stage,
            draw.Id,
            context.Clock,
            new PairingApplicationContext([.. fixtures.Select(fixture => fixture.Id)]),
            []);
        foreach (var match in applyResult.CreatedMatches)
        {
            context.Matches.Add(match);
        }

        return applyResult.CreatedMatches;
    }

    public static void PlayMatches(
        ScenarioContext context,
        IReadOnlyList<Match> matches,
        int count)
    {
        var ordered = matches.OrderBy(m => m.Id.Value).ToList();
        var toPlay = Math.Min(count, ordered.Count);
        for (var i = 0; i < toPlay; i++)
        {
            context.Clock.Advance(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(i));
            var (home, away) = ScoreGenerator.Create(context.Entropy);
            var match = ordered[i];
            match.Start(context.Clock);
            match.Finish(ResultGenerator.Played(home, away), context.Clock);
        }
    }

    public static void PrepareAndStart(ScenarioContext context, Competition competition, Stage stage)
    {
        stage.Prepare(context.Clock);
        competition.Prepare(context.Clock);
        stage.Start(context.Clock);
        competition.Start(context.Clock);
    }

    public static void CompleteRunning(ScenarioContext context, Competition competition, Stage stage)
    {
        if (stage.Status == StageStatus.Running)
        {
            stage.Complete(context.Clock);
        }

        competition.Complete(CompletionMode.Normal, context.Clock);
    }

    /// <summary>
    /// Builds a structured competition (register → configure → materialize → prepare/start → progress).
    /// </summary>
    public static async Task BuildStructuredAsync(
        ScenarioContext context,
        CompetitionRecipe recipe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(recipe);
        cancellationToken.ThrowIfCancellationRequested();

        var competition = CreateCompetitionFromRecipe(context, recipe);
        var entries = RegisterTeams(context, competition, recipe);
        var stage = ConfigurePrimaryStage(context, competition, recipe);
        var matches = MaterializeForFormat(context, competition, stage, recipe, entries);
        PrepareAndStart(context, competition, stage);
        ApplyProgress(context, competition, stage, matches, context.Progress);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Multi-stage Cup demo: QF played + progression fills SF slots; does <strong>not</strong>
    /// call materialize-from-slots (Cockpit / Stage UI owns that step).
    /// </summary>
    public static async Task BuildCupQfSfAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Coupe QF → SF",
            Format = RecipeFormat.Cup,
            TeamCount = 8,
            BracketSize = 8,
            StageName = "Quart de finale",
            TeamNames = TeamNameSource.Generated
        };

        var competition = CreateCompetitionFromRecipe(context, recipe);
        RegisterTeams(context, competition, recipe);
        var quarter = ConfigurePrimaryStage(context, competition, recipe);
        quarter.ReplaceRoundTieFormat(
            quarter.Rounds[0].Id,
            new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
            context.Clock);
        var qfMatches = ApplyCupPairingDeterministic(context, competition, quarter);

        var semi = Stage.Create(
            competition.Id,
            new StageName("Demi-finale"),
            competition.Regulation,
            context.Ids.Stage("sf"),
            context.Clock);
        semi.AddRound("Demi-finales", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), context.Clock);
        foreach (var key in new[] { "SF1-A", "SF1-B", "SF2-A", "SF2-B" })
        {
            semi.AddSlot(key, context.Clock);
        }

        competition.AddStage(semi.Id, context.Clock);
        context.Stages.Add(semi);

        var qfFixtures = quarter.Rounds[0].Fixtures
            .OrderBy(fixture => fixture.Id.Value)
            .Take(4)
            .ToArray();
        if (qfFixtures.Length != 4)
        {
            throw new InvalidOperationException(
                $"Expected 4 QF fixtures for cup-qf-sf, found {qfFixtures.Length}.");
        }

        var destinationKeys = new[] { "SF1-A", "SF1-B", "SF2-A", "SF2-B" };
        var paths = new List<ProgressionPath>(4);
        for (var i = 0; i < 4; i++)
        {
            paths.Add(
                new ProgressionPath(
                    qfFixtures[i].Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(semi.Id, destinationKeys[i])));
        }

        quarter.ReplaceProgressionRules(new ProgressionRules(paths), context.Clock);

        // SF stays Draft (from-slots opportunity). Start competition + QF only.
        quarter.Prepare(context.Clock);
        competition.Prepare(context.Clock);
        quarter.Start(context.Clock);
        competition.Start(context.Clock);

        PlayMatches(context, qfMatches, count: qfMatches.Count);

        var competitionStages = new Stage[] { quarter, semi };
        foreach (var fixture in qfFixtures)
        {
            var legMatches = qfMatches
                .Where(match => fixture.MatchIds.Contains(match.Id))
                .ToArray();
            ApplyProgressionOutcome.Execute(
                quarter,
                fixture.Id,
                legMatches,
                competitionStages,
                context.Clock);
        }

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public static void ApplyProgress(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> matches,
        SeedProgress progress)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matches);

        switch (progress)
        {
            case SeedProgress.Prepared:
                return;
            case SeedProgress.Running:
                PlayMatches(context, matches, count: matches.Count / 2);
                return;
            case SeedProgress.Finished:
                PlayMatches(context, matches, count: matches.Count);
                CompleteRunning(context, competition, stage);
                return;
            default:
                throw new InvalidOperationException($"Unsupported seed progress '{progress}'.");
        }
    }

    private static IReadOnlyList<Match> MaterializeForFormat(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        CompetitionRecipe recipe,
        IReadOnlyList<CompetitionEntry> entries)
        => recipe.Format switch
        {
            RecipeFormat.Groups => AssignThenMaterializeGroups(context, competition, stage, entries),
            RecipeFormat.Cup => ApplyCupPairingDeterministic(context, competition, stage),
            RecipeFormat.Championship => MaterializeChampionshipMatches(context, competition, stage),
            _ => throw new InvalidOperationException($"Unsupported recipe format '{recipe.Format}'.")
        };

    private static IReadOnlyList<Match> AssignThenMaterializeGroups(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        IReadOnlyList<CompetitionEntry> entries)
    {
        AssignGroupsRoundRobin(context, stage, entries);
        return MaterializeGroupsMatches(context, competition, stage);
    }

    private static void EnsureMatchdays(Stage stage, int requiredCount, IClock clock)
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

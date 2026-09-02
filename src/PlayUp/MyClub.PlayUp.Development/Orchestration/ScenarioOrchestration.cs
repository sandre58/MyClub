// -----------------------------------------------------------------------
// <copyright file="ScenarioOrchestration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Development.Generators;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Development.Orchestration;

/// <summary>
/// Internal helpers that orchestrate Domain / Application operations for scenarios (not a fluent builder).
/// </summary>
internal static class ScenarioOrchestration
{
    public static async Task<Competition> CreateCompetitionAsync(
        ScenarioContext context,
        string name,
        Regulation? regulation = null,
        string? shortName = null,
        string? logoAsset = null,
        DateTimeOffset? scheduledStart = null,
        DateTimeOffset? scheduledEnd = null,
        CancellationToken cancellationToken = default)
    {
        var competition = Competition.Create(
            new CompetitionName(name),
            MatchEnrichment.WithDiscipline(regulation ?? BootstrapRegulation.Standard()),
            context.Ids.Competition(),
            context.Clock);
        var logoMediaId = await context.Logos.GetOrImportAsync(logoAsset, cancellationToken).ConfigureAwait(false);
        if (shortName is not null || logoMediaId is not null)
        {
            competition.UpdatePresentation(ShortName.Create(shortName), logoMediaId, context.Clock);
        }

        MatchEnrichment.ApplyRandomCompetitionSchedule(context, competition, scheduledStart, scheduledEnd);

        context.Competitions.Add(competition);
        return competition;
    }

    private static async Task<Competition> CreateCompetitionFromRecipeAsync(
        ScenarioContext context,
        CompetitionRecipe recipe,
        CancellationToken cancellationToken)
    {
        if (recipe.TeamNames != TeamNameSource.Dataset
            || string.IsNullOrWhiteSpace(recipe.DatasetCompetitionKey))
        {
            return await CreateCompetitionAsync(context, recipe.DisplayName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        var dataset = context.Datasets.Get(recipe.DatasetCompetitionKey);
        return await CreateCompetitionAsync(
            context,
            recipe.DisplayName,
            shortName: dataset.ShortName,
            logoAsset: dataset.LogoAsset,
            scheduledStart: dataset.ScheduledStart,
            scheduledEnd: dataset.ScheduledEnd,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<CompetitionEntry>> RegisterTeamsAsync(
        ScenarioContext context,
        Competition competition,
        CompetitionRecipe recipe,
        int? countOverride = null,
        CancellationToken cancellationToken = default)
    {
        var count = countOverride ?? recipe.TeamCount;
        var entries = new List<CompetitionEntry>(count);
        for (var i = 0; i < count; i++)
        {
            context.Clock.Advance(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(i));
            var logoAsset = TeamNameGenerator.ResolveLogoAsset(
                recipe.TeamNames,
                recipe.DatasetCompetitionKey,
                context.Datasets,
                i);
            var logoMediaId = await context.Logos.GetOrImportAsync(logoAsset, cancellationToken).ConfigureAwait(false);
            var (displayName, presentation) = TeamNameGenerator.CreatePresentation(
                context.Entropy,
                i,
                recipe.TeamNames,
                recipe.DatasetCompetitionKey,
                context.Datasets,
                logoMediaId);
            var entry = competition.AddEntry(
                context.Ids.Team($"team-{i}"),
                displayName,
                context.Ids.Entry($"entry-{i}"),
                context.Clock,
                presentation);
            entries.Add(entry);
        }

        MatchEnrichment.SeedRosters(context, competition);
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

        MatchEnrichment.ApplyKickoffs(context, competition, stage, created);
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

        MatchEnrichment.ApplyKickoffs(context, competition, stage, created);
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

        MatchEnrichment.ApplyKickoffs(context, competition, stage, applyResult.CreatedMatches);
        return applyResult.CreatedMatches;
    }

    public static void PlayMatches(
        ScenarioContext context,
        Competition competition,
        IReadOnlyList<Match> matches,
        int count) =>
        MatchEnrichment.PlayMatches(context, competition, matches, count, decisive: false);

    /// <summary>
    /// Plays all matches with a decisive (non-draw) score — required for single-leg KO progression.
    /// </summary>
    public static void PlayDecisiveMatches(
        ScenarioContext context,
        Competition competition,
        IReadOnlyList<Match> matches) =>
        MatchEnrichment.PlayMatches(context, competition, matches, matches.Count, decisive: true);

    public static void PrepareAndStart(ScenarioContext context, Competition competition, Stage stage)
    {
        stage.Prepare(context.Clock);
        competition.Prepare(context.Clock);
        stage.Start(context.Clock);
        competition.Start(context.Clock);
    }

    public static void CompleteRunning(ScenarioContext context, Competition competition, Stage stage) => CompleteAllRunning(context, competition, [stage]);

    /// <summary>
    /// Completes every Running/Suspended stage, then the competition (Normal).
    /// </summary>
    public static void CompleteAllRunning(
        ScenarioContext context,
        Competition competition,
        IEnumerable<Stage> stages)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);

        foreach (var stage in stages)
        {
            if (stage.Status is StageStatus.Running or StageStatus.Suspended)
            {
                stage.Complete(context.Clock);
            }
        }

        competition.Complete(CompletionMode.Normal, context.Clock);
    }

    /// <summary>
    /// Builds a structured competition (register → configure → materialize → prepare/start → progress).
    /// </summary>
    /// <remarks>
    /// Swiss skips upfront materialize: Prepare/Start first, then progressive
    /// <see cref="GenerateNextRound"/> according to <see cref="SeedProgress"/>.
    /// </remarks>
    public static async Task BuildStructuredAsync(
        ScenarioContext context,
        CompetitionRecipe recipe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(recipe);
        cancellationToken.ThrowIfCancellationRequested();

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var stage = ConfigurePrimaryStage(context, competition, recipe);

        if (recipe.Format == RecipeFormat.Swiss)
        {
            PrepareAndStart(context, competition, stage);
            ApplySwissProgress(context, competition, stage, context.Progress);
        }
        else
        {
            var matches = MaterializeForFormat(context, competition, stage, recipe, entries);
            PrepareAndStart(context, competition, stage);
            ApplyProgress(context, competition, stage, matches, context.Progress);
        }

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

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
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

        PlayDecisiveMatches(context, competition, qfMatches);

        var competitionStages = new[] { quarter, semi };
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

    /// <summary>
    /// Coupe de France multi-stage: R32→R16→QF→SF→Final played through; Final PlacementAwards (1–2);
    /// competition Completed with derivable <c>CompetitionOutcome</c>.
    /// Ignores <see cref="ScenarioContext.Progress"/> (fixed seed). Mid-bracket from-slots demo = <c>cup-qf-sf</c>.
    /// </summary>
    public static async Task BuildCoupeDeFranceMultiStageAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Coupe de France",
            Format = RecipeFormat.Cup,
            TeamCount = 32,
            BracketSize = 32,
            StageName = "32es de finale",
            TeamNames = TeamNameSource.Dataset,
            DatasetCompetitionKey = "coupe-de-france"
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var roundOf32 = ConfigurePrimaryStage(context, competition, recipe);
        roundOf32.ReplaceRoundTieFormat(
            roundOf32.Rounds[0].Id,
            new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
            context.Clock);
        var r32Matches = ApplyCupPairingDeterministic(context, competition, roundOf32);

        var r16SlotKeys = PairSlotKeys("R16", pairCount: 8);
        var qfSlotKeys = PairSlotKeys("QF", pairCount: 4);
        var sfSlotKeys = PairSlotKeys("SF", pairCount: 2);
        var finalSlotKeys = new[] { "F-A", "F-B" };

        var roundOf16 = CreateKnockoutStage(
            context, competition, "r16", "16es de finale", "16es de finale", r16SlotKeys);
        var quarter = CreateKnockoutStage(
            context, competition, "qf", "Quarts de finale", "Quarts de finale", qfSlotKeys);
        var semi = CreateKnockoutStage(
            context, competition, "sf", "Demis de finale", "Demis de finale", sfSlotKeys);
        var final = CreateKnockoutStage(
            context, competition, "final", "Finale", "Finale", finalSlotKeys);

        Stage[] allStages = [roundOf32, roundOf16, quarter, semi, final];

        var r32Fixtures = roundOf32.Rounds[0].Fixtures
            .OrderBy(fixture => fixture.Id.Value)
            .Take(16)
            .ToArray();
        if (r32Fixtures.Length != 16)
        {
            throw new InvalidOperationException(
                $"Expected 16 R32 fixtures for coupe-de-france, found {r32Fixtures.Length}.");
        }

        WireWinnerProgression(roundOf32, roundOf16, r32Fixtures, r16SlotKeys, context.Clock);

        roundOf32.Prepare(context.Clock);
        competition.Prepare(context.Clock);
        roundOf32.Start(context.Clock);
        competition.Start(context.Clock);

        PlayDecisiveMatches(context, competition, r32Matches);
        ApplyAllProgressions(context, roundOf32, r32Fixtures, r32Matches, allStages);

        PlayKnockoutRound(
            context,
            competition,
            roundOf16,
            AdjacentPairs(r16SlotKeys),
            expectedFixtures: 8,
            nextStage: quarter,
            nextSlotKeys: qfSlotKeys,
            allStages);
        PlayKnockoutRound(
            context,
            competition,
            quarter,
            AdjacentPairs(qfSlotKeys),
            expectedFixtures: 4,
            nextStage: semi,
            nextSlotKeys: sfSlotKeys,
            allStages);
        PlayKnockoutRound(
            context,
            competition,
            semi,
            AdjacentPairs(sfSlotKeys),
            expectedFixtures: 2,
            nextStage: final,
            nextSlotKeys: finalSlotKeys,
            allStages);

        var finalMatches = MaterializeFromSlots(context, competition, final, AdjacentPairs(finalSlotKeys));
        var finalFixture = OrderedFixtures(final, expectedCount: 1)[0];
        WireFinalPlacementAwards(final, finalFixture, context.Clock);
        PrepareAndStartStage(context, final);
        PlayDecisiveMatches(context, competition, finalMatches);

        CompleteAllRunning(context, competition, allStages);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// World Cup: Groups 8×4 → Top2 → R16→QF→SF → Final + Bronze played through;
    /// PlacementAwards ranks 1–4; competition Completed with derivable <c>CompetitionOutcome</c>.
    /// Ignores progress (fixed seed). Mid-bracket from-slots demo = <c>cup-qf-sf</c>.
    /// </summary>
    public static async Task BuildWorldCupAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "FIFA World Cup",
            Format = RecipeFormat.Groups,
            TeamCount = 32,
            GroupCount = 8,
            ParticipantsPerGroup = 4,
            StageName = "Phase de groupes",
            TeamNames = TeamNameSource.Dataset,
            DatasetCompetitionKey = "world-cup"
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var groups = ConfigurePrimaryStage(context, competition, recipe);

        var r16SlotKeys = WorldCupR16SlotKeys;
        var r16Pairs = WorldCupR16Pairs;
        var qfSlotKeys = PairSlotKeys("QF", pairCount: 4);
        var sfSlotKeys = PairSlotKeys("SF", pairCount: 2);
        var finalSlotKeys = new[] { "F-A", "F-B" };
        var bronzeSlotKeys = new[] { "B-A", "B-B" };

        var roundOf16 = CreateKnockoutStage(
            context, competition, "r16", "Huitièmes de finale", "Huitièmes de finale", r16SlotKeys);
        var quarter = CreateKnockoutStage(
            context, competition, "qf", "Quarts de finale", "Quarts de finale", qfSlotKeys);
        var semi = CreateKnockoutStage(
            context, competition, "sf", "Demis de finale", "Demis de finale", sfSlotKeys);
        var final = CreateKnockoutStage(
            context, competition, "final", "Finale", "Finale", finalSlotKeys);
        var bronze = CreateKnockoutStage(
            context, competition, "bronze", "Match pour la 3e place", "Match pour la 3e place", bronzeSlotKeys);

        WireWorldCupQualification(groups, roundOf16, context.Clock);

        var groupMatches = AssignThenMaterializeGroups(context, competition, groups, entries);
        PrepareAndStart(context, competition, groups);
        PlayMatches(context, competition, groupMatches, count: groupMatches.Count);

        var groupStandings = new Dictionary<GroupId, Standing>();
        foreach (var group in groups.Groups)
        {
            groupStandings[group.Id] = CalculateStanding.Execute(
                group.EntryIds,
                groupMatches,
                groups.Regulation.StandingRules);
        }

        Stage[] allStages = [groups, roundOf16, quarter, semi, final, bronze];
        ApplyQualification.Execute(
            groups,
            overallStanding: null,
            groupStandings,
            allStages,
            context.Clock);

        PlayKnockoutRound(
            context,
            competition,
            roundOf16,
            r16Pairs,
            expectedFixtures: 8,
            nextStage: quarter,
            nextSlotKeys: qfSlotKeys,
            allStages);
        PlayKnockoutRound(
            context,
            competition,
            quarter,
            AdjacentPairs(qfSlotKeys),
            expectedFixtures: 4,
            nextStage: semi,
            nextSlotKeys: sfSlotKeys,
            allStages);

        var sfMatches = MaterializeFromSlots(context, competition, semi, AdjacentPairs(sfSlotKeys));
        var sfFixtures = OrderedFixtures(semi, expectedCount: 2);
        WireSemiToFinalAndBronze(semi, final, bronze, sfFixtures, context.Clock);
        PrepareAndStartStage(context, semi);
        PlayDecisiveMatches(context, competition, sfMatches);
        ApplyAllProgressions(context, semi, sfFixtures, sfMatches, allStages);

        var finalMatches = MaterializeFromSlots(context, competition, final, AdjacentPairs(finalSlotKeys));
        var bronzeMatches = MaterializeFromSlots(context, competition, bronze, AdjacentPairs(bronzeSlotKeys));
        var finalFixture = OrderedFixtures(final, expectedCount: 1)[0];
        var bronzeFixture = OrderedFixtures(bronze, expectedCount: 1)[0];
        WireFinalAndBronzePlacementAwards(final, finalFixture, bronze, bronzeFixture, context.Clock);
        PrepareAndStartStage(context, final);
        PrepareAndStartStage(context, bronze);
        PlayDecisiveMatches(context, competition, finalMatches);
        PlayDecisiveMatches(context, competition, bronzeMatches);

        CompleteAllRunning(context, competition, allStages);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Materialize → wire Winner progression to next → start → play → apply progression.
    /// </summary>
    private static void PlayKnockoutRound(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        IReadOnlyList<CupSlotPair> pairs,
        int expectedFixtures,
        Stage nextStage,
        string[] nextSlotKeys,
        IReadOnlyList<Stage> allStages)
    {
        var matches = MaterializeFromSlots(context, competition, stage, pairs);
        var fixtures = OrderedFixtures(stage, expectedFixtures);
        WireWinnerProgression(stage, nextStage, fixtures, nextSlotKeys, context.Clock);
        PrepareAndStartStage(context, stage);
        PlayDecisiveMatches(context, competition, matches);
        ApplyAllProgressions(context, stage, fixtures, matches, allStages);
    }

    private static void WireFinalPlacementAwards(Stage final, Fixture finalFixture, IClock clock) =>
        final.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath(finalFixture.Id, ProgressionOutcome.Winner, rank: 1),
                new PlacementAwardPath(finalFixture.Id, ProgressionOutcome.Loser, rank: 2)
            ]),
            clock);

    private static void WireFinalAndBronzePlacementAwards(
        Stage final,
        Fixture finalFixture,
        Stage bronze,
        Fixture bronzeFixture,
        IClock clock)
    {
        WireFinalPlacementAwards(final, finalFixture, clock);
        bronze.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath(bronzeFixture.Id, ProgressionOutcome.Winner, rank: 3),
                new PlacementAwardPath(bronzeFixture.Id, ProgressionOutcome.Loser, rank: 4)
            ]),
            clock);
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
                PlayMatches(context, competition, matches, count: 0);
                return;
            case SeedProgress.Running:
                PlayMatches(context, competition, matches, count: matches.Count / 2);
                return;
            case SeedProgress.Finished:
                PlayMatches(context, competition, matches, count: matches.Count);
                CompleteRunning(context, competition, stage);
                return;
            default:
                throw new InvalidOperationException($"Unsupported seed progress '{progress}'.");
        }
    }

    /// <summary>
    /// Swiss progress: rounds are created only after Running via GenerateNextRound.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>prepared</c> — Running, 0 rounds (Cockpit ready for GenerateNextRound).</item>
    /// <item><c>running</c> — round 1 finished + round 2 generated, half played (awaiting results).</item>
    /// <item><c>finished</c> — all planned rounds generated and finished; competition Completed.</item>
    /// </list>
    /// </remarks>
    public static void ApplySwissProgress(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        SeedProgress progress)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        if (!stage.IsSwiss || stage.SwissSettings is null)
        {
            throw new InvalidOperationException("ApplySwissProgress requires a Swiss stage with SwissSettings.");
        }

        var planned = stage.SwissSettings.RoundCount;
        var allMatches = new List<Match>();

        switch (progress)
        {
            case SeedProgress.Prepared:
                return;
            case SeedProgress.Running:
                {
                    // Round 1 complete → round 2 open with ~50% results (awaiting next GenerateNextRound).
                    var round1 = GenerateSwissRound(context, competition, stage, allMatches);
                    PlayMatches(context, competition, round1, count: round1.Count);
                    var round2 = GenerateSwissRound(context, competition, stage, allMatches);
                    PlayMatches(context, competition, round2, count: Math.Max(1, round2.Count / 2));
                    return;
                }

            case SeedProgress.Finished:
                {
                    for (var round = 1; round <= planned; round++)
                    {
                        var created = GenerateSwissRound(context, competition, stage, allMatches);
                        PlayMatches(context, competition, created, count: created.Count);
                    }

                    CompleteRunning(context, competition, stage);
                    return;
                }

            default:
                throw new InvalidOperationException($"Unsupported seed progress '{progress}'.");
        }
    }

    private static IReadOnlyList<Match> GenerateSwissRound(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        List<Match> accumulated)
    {
        var result = GenerateNextRound.Execute(competition, stage, accumulated, context.Clock);
        foreach (var match in result.CreatedMatches)
        {
            context.Matches.Add(match);
            accumulated.Add(match);
        }

        MatchEnrichment.ApplyKickoffs(context, competition, stage, result.CreatedMatches);
        return result.CreatedMatches;
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
            RecipeFormat.Swiss => throw new InvalidOperationException(
                "Swiss does not use MaterializeForFormat — ApplySwissProgress after PrepareAndStart."),
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

    private static Stage CreateKnockoutStage(
        ScenarioContext context,
        Competition competition,
        string stageKey,
        string stageName,
        string roundName,
        IReadOnlyList<string> slotKeys)
    {
        var stage = Stage.Create(
            competition.Id,
            new StageName(stageName),
            competition.Regulation,
            context.Ids.Stage(stageKey),
            context.Clock);
        stage.AddRound(roundName, new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), context.Clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key, context.Clock);
        }

        competition.AddStage(stage.Id, context.Clock);
        context.Stages.Add(stage);
        return stage;
    }

    private static string[] PairSlotKeys(string prefix, int pairCount)
    {
        var keys = new string[pairCount * 2];
        for (var i = 0; i < pairCount; i++)
        {
            keys[i * 2] = $"{prefix}-{i + 1}-A";
            keys[(i * 2) + 1] = $"{prefix}-{i + 1}-B";
        }

        return keys;
    }

    private static CupSlotPair[] AdjacentPairs(string[] slotKeys)
    {
        if (slotKeys.Length % 2 != 0)
        {
            throw new InvalidOperationException("Slot keys must come in pairs for from-slots materialization.");
        }

        var pairs = new CupSlotPair[slotKeys.Length / 2];
        for (var i = 0; i < pairs.Length; i++)
        {
            pairs[i] = new CupSlotPair(slotKeys[i * 2], slotKeys[(i * 2) + 1]);
        }

        return pairs;
    }

    /// <summary>Classic WC R16 matrix (Top2): A1–B2, C1–D2, then B1–A2, and so on.</summary>
    private static readonly string[] WorldCupR16SlotKeys =
    [
        "A1", "B2", "C1", "D2", "E1", "F2", "G1", "H2",
        "B1", "A2", "D1", "C2", "F1", "E2", "H1", "G2"
    ];

    private static readonly CupSlotPair[] WorldCupR16Pairs =
    [
        new("A1", "B2"),
        new("C1", "D2"),
        new("E1", "F2"),
        new("G1", "H2"),
        new("B1", "A2"),
        new("D1", "C2"),
        new("F1", "E2"),
        new("H1", "G2")
    ];

    private static void WireWorldCupQualification(Stage groups, Stage roundOf16, IClock clock)
    {
        var orderedGroups = groups.Groups.OrderBy(group => group.Name, StringComparer.Ordinal).ToArray();
        if (orderedGroups.Length != 8)
        {
            throw new InvalidOperationException(
                $"Expected 8 World Cup groups, found {orderedGroups.Length}.");
        }

        var paths = new List<QualificationPath>(16);
        var order = 1;
        foreach (var group in orderedGroups)
        {
            var letter = group.Name;
            paths.Add(
                new QualificationPath(
                    order++,
                    QualificationSource.FromGroup(group.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    new QualificationDestination(roundOf16.Id, $"{letter}1")));
            paths.Add(
                new QualificationPath(
                    order++,
                    QualificationSource.FromGroup(group.Id),
                    new QualificationSelection(SelectionMode.Position, 2),
                    new QualificationDestination(roundOf16.Id, $"{letter}2")));
        }

        groups.ReplaceQualificationRules(new QualificationRules(paths), clock);
    }

    private static void WireWinnerProgression(
        Stage source,
        Stage destination,
        Fixture[] fixtures,
        string[] destinationKeys,
        IClock clock)
    {
        if (fixtures.Length != destinationKeys.Length)
        {
            throw new InvalidOperationException(
                $"Progression wiring expects {destinationKeys.Length} fixtures, found {fixtures.Length}.");
        }

        var paths = new List<ProgressionPath>(fixtures.Length);
        paths.AddRange(fixtures.Select((t, i) => new ProgressionPath(t.Id, ProgressionOutcome.Winner, new ProgressionDestination(destination.Id, destinationKeys[i]))));

        source.ReplaceProgressionRules(new ProgressionRules(paths), clock);
    }

    private static void WireSemiToFinalAndBronze(
        Stage semi,
        Stage final,
        Stage bronze,
        Fixture[] sfFixtures,
        IClock clock)
    {
        if (sfFixtures.Length != 2)
        {
            throw new InvalidOperationException(
                $"Expected 2 SF fixtures for Final+Bronze wiring, found {sfFixtures.Length}.");
        }

        var paths = new ProgressionPath[]
        {
            new(sfFixtures[0].Id, ProgressionOutcome.Winner, new ProgressionDestination(final.Id, "F-A")),
            new(sfFixtures[1].Id, ProgressionOutcome.Winner, new ProgressionDestination(final.Id, "F-B")),
            new(sfFixtures[0].Id, ProgressionOutcome.Loser, new ProgressionDestination(bronze.Id, "B-A")),
            new(sfFixtures[1].Id, ProgressionOutcome.Loser, new ProgressionDestination(bronze.Id, "B-B"))
        };
        semi.ReplaceProgressionRules(new ProgressionRules(paths), clock);
    }

    private static Fixture[] OrderedFixtures(Stage stage, int expectedCount)
    {
        var fixtures = stage.Rounds[0].Fixtures
            .OrderBy(fixture => fixture.Id.Value)
            .Take(expectedCount)
            .ToArray();
        return fixtures.Length != expectedCount
            ? throw new InvalidOperationException(
                $"Expected {expectedCount} fixtures on stage '{stage.Name.Value}', found {fixtures.Length}.")
            : fixtures;
    }

    private static IReadOnlyList<Match> MaterializeFromSlots(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        IReadOnlyList<CupSlotPair> pairs)
    {
        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            pairs,
            [],
            context.Clock);
        foreach (var match in result.CreatedMatches)
        {
            context.Matches.Add(match);
        }

        MatchEnrichment.ApplyKickoffs(context, competition, stage, result.CreatedMatches);
        return result.CreatedMatches;
    }

    private static void PrepareAndStartStage(ScenarioContext context, Stage stage)
    {
        stage.Prepare(context.Clock);
        stage.Start(context.Clock);
    }

    private static void ApplyAllProgressions(
        ScenarioContext context,
        Stage source,
        IReadOnlyList<Fixture> fixtures,
        IReadOnlyList<Match> matches,
        IReadOnlyList<Stage> competitionStages)
    {
        foreach (var fixture in fixtures)
        {
            var legMatches = matches
                .Where(match => fixture.MatchIds.Contains(match.Id))
                .ToArray();
            ApplyProgressionOutcome.Execute(
                source,
                fixture.Id,
                legMatches,
                competitionStages,
                context.Clock);
        }
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

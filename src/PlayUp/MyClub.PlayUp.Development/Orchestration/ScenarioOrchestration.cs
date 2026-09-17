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
        CancellationToken cancellationToken,
        Regulation? regulation = null)
    {
        if (recipe.TeamNames != TeamNameSource.Dataset
            || string.IsNullOrWhiteSpace(recipe.DatasetCompetitionKey))
        {
            return await CreateCompetitionAsync(
                    context,
                    recipe.DisplayName,
                    regulation: regulation,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        var dataset = context.Datasets.Get(recipe.DatasetCompetitionKey);
        return await CreateCompetitionAsync(
            context,
            recipe.DisplayName,
            regulation: regulation,
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
            var (displayName, presentation) = TeamNameGenerator.CreatePresentation(i,
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

    /// <summary>
    /// Seeds the root composition entry set (Affectation) from registered teams.
    /// </summary>
    /// <param name="stage">Root stage.</param>
    /// <param name="entries">Competition entries (Active preferred).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <param name="take">Optional partial take (first N Active) for incomplete composition QA.</param>
    public static void AssignRootComposition(
        Stage stage,
        IReadOnlyList<CompetitionEntry> entries,
        IClock clock,
        int? take = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(clock);

        var active = entries.Where(entry => entry.Status == EntryStatus.Active);
        var selected = take is { } limit ? active.Take(limit) : active;
        stage.ReplaceCompositionEntries([.. selected.Select(entry => entry.Id)], clock);
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
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing(entries));

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

    /// <summary>
    /// Transitions stage + competition to Ready without starting.
    /// </summary>
    public static void PrepareOnly(ScenarioContext context, Competition competition, Stage stage)
    {
        stage.Prepare(context.Clock);
        competition.Prepare(context.Clock);
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
    /// Builds a structured competition (register → configure → materialize → lifecycle).
    /// </summary>
    /// <remarks>
    /// Swiss skips upfront materialize: Ready/Progressive Prepare(/Start) first, then progressive
    /// <see cref="GenerateNextRound"/> according to <see cref="SeedProgress"/> when lifecycle is Progressive.
    /// </remarks>
    public static async Task BuildStructuredAsync(
        ScenarioContext context,
        CompetitionRecipe recipe,
        StructuredSeedLifecycle lifecycle = StructuredSeedLifecycle.Progressive,
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
        AssignRootComposition(stage, entries, context.Clock);

        if (recipe.Format == RecipeFormat.Swiss)
        {
            ApplyStructuredLifecycleSwiss(context, competition, stage, lifecycle);
        }
        else
        {
            var matches = MaterializeForFormat(context, competition, stage, recipe, entries);
            ApplyStructuredLifecycle(context, competition, stage, matches, lifecycle);
        }

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Groups stage with pot DrawRules and empty groups — Structure “draw pending”, stays Draft.
    /// </summary>
    public static async Task BuildGroupsDrawPendingAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Groupes — tirage en attente",
            Format = RecipeFormat.Groups,
            TeamCount = 16,
            GroupCount = 4,
            PlacesPerGroup = 4,
            StageName = "Phase de groupes",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var stage = ConfigurePrimaryStage(context, competition, recipe);
        AssignRootComposition(stage, entries, context.Clock);
        stage.ReplaceDrawRules(
            new DrawRules(DrawMode.Random, potRules: new PotRules(4)),
            context.Clock);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Cup bracket structured, entries registered, composition <strong>empty</strong>, draw not created —
    /// Draft E0 (Constituer les entrées) + tirage pending.
    /// </summary>
    public static async Task BuildCupDrawPendingAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Coupe — tirage en attente",
            Format = RecipeFormat.Cup,
            TeamCount = 16,
            BracketSize = 16,
            StageName = "Tour à élimination",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var stage = ConfigurePrimaryStage(context, competition, recipe);

        // DrawRules are not seeded by ConfigureStructure — engage tirage for this scenario.
        stage.ReplaceDrawRules(new DrawRules(DrawMode.Random), context.Clock);

        // Intentionally no AssignRootComposition — Structure Entrées E0 (0 / 16).
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Cup 16 with partial composition (10 / 16) — Draft E1 Completer les entrées.
    /// </summary>
    public static async Task BuildCupCompositionPartialAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Coupe — composition partielle",
            Format = RecipeFormat.Cup,
            TeamCount = 16,
            BracketSize = 16,
            StageName = "Tour à élimination",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var stage = ConfigurePrimaryStage(context, competition, recipe);
        AssignRootComposition(stage, entries, context.Clock, take: 10);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Cup 16 with full composition (16 / 16) — Draft E2 Modifier les entrées + tirage pending.
    /// </summary>
    public static async Task BuildCupCompositionCompleteAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Coupe — composition complète",
            Format = RecipeFormat.Cup,
            TeamCount = 16,
            BracketSize = 16,
            StageName = "Tour à élimination",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var stage = ConfigurePrimaryStage(context, competition, recipe);
        AssignRootComposition(stage, entries, context.Clock);
        stage.ReplaceDrawRules(new DrawRules(DrawMode.Random), context.Clock);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Championship Running mid-state then one withdrawal (forfait) — Domain allows Withdraw only when Running/Suspended.
    /// </summary>
    public static async Task BuildRegistrationWithdrawnAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Championnat — forfait",
            Format = RecipeFormat.Championship,
            TeamCount = 8,
            MatchdayCount = 7,
            StageName = "Championnat",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var stage = ConfigurePrimaryStage(context, competition, recipe);
        var matches = MaterializeForFormat(context, competition, stage, recipe, entries);
        PrepareAndStart(context, competition, stage);
        ApplyProgress(context, competition, stage, matches, SeedProgress.Running);

        var withdrawn = entries.OrderBy(entry => entry.Id.Value).First();
        competition.WithdrawEntry(withdrawn.Id, context.Clock);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Healthy multi-phase mid-state: Groups 2×4 finished → Top2 → QF population → Slot Draw; KO stays Draft.
    /// Structure UX case #9 — Affectation racine (groups composition) + inbound Qualif on QF
    /// (Entrées aval = WhoFeeds lecture → jump / edit on source). Qual V2: no Qual→slot.
    /// </summary>
    public static async Task BuildGroupsToKoMidAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Groupes → QF (mi-parcours)",
            Format = RecipeFormat.Groups,
            TeamCount = 8,
            GroupCount = 2,
            PlacesPerGroup = 4,
            StageName = "Phase de groupes",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var groups = ConfigurePrimaryStage(context, competition, recipe);

        var qfSlotKeys = PairSlotKeys("QF", pairCount: 2);
        var quarter = CreateKnockoutStage(
            context, competition, "qf", "Quarts de finale", "Quarts de finale", qfSlotKeys);
        MatchEnrichment.SpecializeWithExtraTimeAndPenalties(quarter, context.Clock);

        WireEachGroupQualificationToPopulation(
            context.Ids, groups, quarter, positionFrom: 1, positionTo: 2, context.Clock);

        var groupMatches = AssignThenMaterializeGroups(context, competition, groups, entries);
        PrepareAndStart(context, competition, groups);
        PlayMatches(context, competition, groupMatches, count: groupMatches.Count);

        var groupStandings = new Dictionary<GroupId, Standing>();
        foreach (var group in groups.Groups)
        {
            groupStandings[group.Id] = CalculateStanding.Execute(
                group.EntryIds,
                groupMatches,
                groups.Regulation.StandingRules ?? BootstrapRegulation.Standard().StandingRules);
        }

        ApplyQualification.Execute(
            groups,
            overallStanding: null,
            groupStandings,
            [groups, quarter],
            context.Clock);

        PlacePopulationIntoSlotsViaDraw(context, competition, quarter, qfSlotKeys);

        groups.Complete(context.Clock);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Structure flux QA — Affectation racine + Sorties Qualification configurées, reste Draft.
    /// Groups 2×4 → QF (Top1/Top2 → population) ; pas de matchs joués. Aval QF = population + Draw (pas Qual→slot).
    /// </summary>
    public static async Task BuildFluxQualifDraftAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Flux — Qualification Draft",
            Format = RecipeFormat.Groups,
            TeamCount = 8,
            GroupCount = 2,
            PlacesPerGroup = 4,
            StageName = "Phase de groupes",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var groups = ConfigurePrimaryStage(context, competition, recipe);
        groups.ReplaceDrawRules(null, context.Clock);
        AssignRootComposition(groups, entries, context.Clock);

        var qfSlotKeys = PairSlotKeys("QF", pairCount: 2);
        var quarter = CreateKnockoutStage(
            context, competition, "qf", "Quarts de finale", "Quarts de finale", qfSlotKeys);

        WireEachGroupQualificationToPopulation(
            context.Ids, groups, quarter, positionFrom: 1, positionTo: 2, context.Clock);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Structure flux QA — Sorties Progression (Winner/Loser) + Attribution 1–4, reste Draft.
    /// Demi (Affectation 4) → Finale + Bronze ; fixtures créées pour lier les chemins.
    /// </summary>
    public static async Task BuildFluxProgPlacementDraftAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Flux — Progression + Attribution Draft",
            Format = RecipeFormat.Cup,
            TeamCount = 4,
            BracketSize = 4,
            StageName = "Demi-finales",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var sfSlotKeys = PairSlotKeys("SF", pairCount: 2);
        var semi = CreateKnockoutStage(
            context, competition, "sf", "Demi-finales", "Demi-finales", sfSlotKeys);
        AssignRootComposition(semi, entries, context.Clock);
        var sfFixtures = AddRoundFixtures(semi, count: 2, context.Clock);

        var final = CreateKnockoutStage(
            context, competition, "final", "Finale", "Finale", ["F-A", "F-B"]);
        var bronze = CreateKnockoutStage(
            context, competition, "bronze", "Match pour la 3e place", "Match pour la 3e place", ["B-A", "B-B"]);

        WireSemiToFinalAndBronze(semi, final, bronze, sfFixtures, context.Clock);

        var finalFixture = AddRoundFixtures(final, count: 1, context.Clock)[0];
        var bronzeFixture = AddRoundFixtures(bronze, count: 1, context.Clock)[0];
        WireFinalAndBronzePlacementAwards(final, finalFixture, bronze, bronzeFixture, context.Clock);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Structure flux QA — multi-phases sans arêtes (Sorties / Attribution vides).
    /// Overflow « Ajouter une sortie » / « Ajouter une attribution » depuis la fiche.
    /// </summary>
    public static async Task BuildFluxEmptyRelationsDraftAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Flux — Relations vides Draft",
            Format = RecipeFormat.Groups,
            TeamCount = 8,
            GroupCount = 2,
            PlacesPerGroup = 4,
            StageName = "Phase de groupes",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var groups = ConfigurePrimaryStage(context, competition, recipe);
        groups.ReplaceDrawRules(null, context.Clock);
        AssignRootComposition(groups, entries, context.Clock);

        _ = CreateKnockoutStage(
            context,
            competition,
            "qf",
            "Quarts de finale",
            "Quarts de finale",
            PairSlotKeys("QF", pairCount: 2));

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Structure flux QA — graphe complet Draft : Qualif + Progression + Attribution.
    /// Groups → Demis (Top2) → Finale/Bronze ; Affectation racine ; aucun match joué.
    /// </summary>
    public static async Task BuildFluxFullGraphDraftAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Flux — Graphe complet Draft",
            Format = RecipeFormat.Groups,
            TeamCount = 8,
            GroupCount = 2,
            PlacesPerGroup = 4,
            StageName = "Phase de groupes",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var groups = ConfigurePrimaryStage(context, competition, recipe);
        groups.ReplaceDrawRules(null, context.Clock);
        AssignRootComposition(groups, entries, context.Clock);

        var sfSlotKeys = PairSlotKeys("SF", pairCount: 2);
        var semi = CreateKnockoutStage(
            context, competition, "sf", "Demi-finales", "Demi-finales", sfSlotKeys);
        var sfFixtures = AddRoundFixtures(semi, count: 2, context.Clock);

        var final = CreateKnockoutStage(
            context, competition, "final", "Finale", "Finale", ["F-A", "F-B"]);
        var bronze = CreateKnockoutStage(
            context, competition, "bronze", "Match pour la 3e place", "Match pour la 3e place", ["B-A", "B-B"]);
        var finalFixture = AddRoundFixtures(final, count: 1, context.Clock)[0];
        var bronzeFixture = AddRoundFixtures(bronze, count: 1, context.Clock)[0];

        WireEachGroupQualificationToPopulation(
            context.Ids, groups, semi, positionFrom: 1, positionTo: 2, context.Clock);

        WireSemiToFinalAndBronze(semi, final, bronze, sfFixtures, context.Clock);
        WireFinalAndBronzePlacementAwards(final, finalFixture, bronze, bronzeFixture, context.Clock);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Multi-stage Cup Running: QF played → SF materialized and ~half played (healthy ops mid-bracket).
    /// </summary>
    public static async Task BuildCupSfRunningAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Coupe QF → SF (running)",
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
            semi.AddSlot(key);
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
                $"Expected 4 QF fixtures for cup-sf-running, found {qfFixtures.Length}.");
        }

        var destinationKeys = new[] { "SF1-A", "SF1-B", "SF2-A", "SF2-B" };
        var paths = new List<ProgressionPath>(4);
        for (var i = 0; i < 4; i++)
        {
            paths.Add(
                new ProgressionPath(
                    qfFixtures[i].Id,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(semi.Id)));
        }

        quarter.ReplaceProgressionRules(new ProgressionRules(paths), context.Clock);

        quarter.Prepare(context.Clock);
        competition.Prepare(context.Clock);
        quarter.Start(context.Clock);
        competition.Start(context.Clock);

        PlayDecisiveMatches(context, competition, qfMatches);

        Stage[] competitionStages = [quarter, semi];
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

        PlacePopulationEntriesIntoSlots(semi, destinationKeys, context.Clock);

        quarter.Complete(context.Clock);

        var sfPairs = AdjacentPairs(destinationKeys);
        var sfMatches = MaterializeFromSlots(context, competition, semi, sfPairs);
        PrepareAndStartStage(context, semi);
        PlayMatches(context, competition, sfMatches, count: Math.Max(1, sfMatches.Count / 2));

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Championship structure assigned + materialize, stays Draft (healthy Structure edit surface).
    /// </summary>
    public static async Task BuildChampionshipStructureDraftAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Championnat — structure Draft",
            Format = RecipeFormat.Championship,
            TeamCount = 8,
            MatchdayCount = 7,
            StageName = "Championnat",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var stage = ConfigurePrimaryStage(context, competition, recipe);
        AssignRootComposition(stage, entries, context.Clock);
        _ = MaterializeForFormat(context, competition, stage, recipe, entries);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Draft multi-phase graph with intentional Structure validity issues (dangling Qual population
    /// targets) and a multi-destination progression — Topology / anomaly QA, stays Draft.
    /// </summary>
    public static async Task BuildStructureGraphInvalidAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Structure — graphe invalide",
            Format = RecipeFormat.Groups,
            TeamCount = 8,
            GroupCount = 2,
            PlacesPerGroup = 4,
            StageName = "Poules",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var groups = ConfigurePrimaryStage(context, competition, recipe);
        groups.ReplaceDrawRules(null, context.Clock);

        var barrages = CreateKnockoutStage(
            context,
            competition,
            "barrages",
            "Barrages",
            "Barrages",
            ["BR-1-A", "BR-1-B"]);
        var finale = CreateKnockoutStage(
            context,
            competition,
            "final",
            "Finale",
            "Finale",
            ["F-A", "F-B"]);
        var bronze = CreateKnockoutStage(
            context,
            competition,
            "bronze",
            "Match pour la 3e place",
            "Match pour la 3e place",
            ["BRZ-A", "BRZ-B"]);

        // Two valid population destinations + two dangling stage targets → DanglingQualificationTarget.
        var ghostStageId = StageId.New();
        var orderedGroups = groups.Groups.OrderBy(group => group.Name, StringComparer.Ordinal).ToArray();
        if (orderedGroups.Length != 2)
        {
            throw new InvalidOperationException(
                $"Expected 2 groups for structure-graph-invalid, found {orderedGroups.Length}.");
        }

        var groupOrder = orderedGroups.Select(group => group.Id).ToArray();
        groups.ReplaceQualificationRules(
            QualificationRules.FromIntents(
            [
                new QualificationIntent(
                    context.Ids.Intent("qual-br-g0-p1"),
                    order: 1,
                    QualificationIntentSourceKind.SingleGroup,
                    positionFrom: 1,
                    positionTo: 1,
                    barrages.Id,
                    groupId: orderedGroups[0].Id),
                new QualificationIntent(
                    context.Ids.Intent("qual-br-g1-p1"),
                    order: 2,
                    QualificationIntentSourceKind.SingleGroup,
                    positionFrom: 1,
                    positionTo: 1,
                    barrages.Id,
                    groupId: orderedGroups[1].Id),
                new QualificationIntent(
                    context.Ids.Intent("qual-ghost-g0-p2"),
                    order: 3,
                    QualificationIntentSourceKind.SingleGroup,
                    positionFrom: 2,
                    positionTo: 2,
                    ghostStageId,
                    groupId: orderedGroups[0].Id),
                new QualificationIntent(
                    context.Ids.Intent("qual-ghost-g1-p2"),
                    order: 4,
                    QualificationIntentSourceKind.SingleGroup,
                    positionFrom: 2,
                    positionTo: 2,
                    ghostStageId,
                    groupId: orderedGroups[1].Id)
            ],
            groupOrder),
            context.Clock);

        var barragesFixture = barrages.AddFixture(barrages.Rounds[0].Id, context.Clock);
        barrages.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    barragesFixture.Id,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(finale.Id)),
                new ProgressionPath(
                    barragesFixture.Id,
                    ProgressionOutcome.Loser,
                    ProgressionDestination.ForPopulation(bronze.Id))
            ]),
            context.Clock);

        AssignGroupsRoundRobin(groups, entries);
        _ = MaterializeGroupsMatches(context, competition, groups);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ApplyStructuredLifecycle(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> matches,
        StructuredSeedLifecycle lifecycle)
    {
        switch (lifecycle)
        {
            case StructuredSeedLifecycle.Ready:
                PrepareOnly(context, competition, stage);
                return;
            case StructuredSeedLifecycle.Suspended:
                PrepareAndStart(context, competition, stage);
                ApplyProgress(context, competition, stage, matches, SeedProgress.Running);
                stage.Suspend(context.Clock);
                competition.Suspend(context.Clock);
                return;
            case StructuredSeedLifecycle.Archived:
                PrepareAndStart(context, competition, stage);
                ApplyProgress(context, competition, stage, matches, SeedProgress.Finished);
                competition.Archive(context.Clock);
                return;
            case StructuredSeedLifecycle.Progressive:
                PrepareAndStart(context, competition, stage);
                ApplyProgress(context, competition, stage, matches, context.Progress);
                return;
            default:
                throw new InvalidOperationException($"Unsupported structured lifecycle '{lifecycle}'.");
        }
    }

    private static void ApplyStructuredLifecycleSwiss(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        StructuredSeedLifecycle lifecycle)
    {
        switch (lifecycle)
        {
            case StructuredSeedLifecycle.Ready:
                PrepareOnly(context, competition, stage);
                return;
            case StructuredSeedLifecycle.Suspended:
                PrepareAndStart(context, competition, stage);
                ApplySwissProgress(context, competition, stage, SeedProgress.Running);
                stage.Suspend(context.Clock);
                competition.Suspend(context.Clock);
                return;
            case StructuredSeedLifecycle.Archived:
                PrepareAndStart(context, competition, stage);
                ApplySwissProgress(context, competition, stage, SeedProgress.Finished);
                competition.Archive(context.Clock);
                return;
            case StructuredSeedLifecycle.Progressive:
                PrepareAndStart(context, competition, stage);
                ApplySwissProgress(context, competition, stage, context.Progress);
                return;
            default:
                throw new InvalidOperationException($"Unsupported structured lifecycle '{lifecycle}'.");
        }
    }

    /// <summary>
    /// Multi-stage Cup demo: QF played + progression fills SF slots; does <strong>not</strong>
    /// call materialize-from-slots (Overview / Stage UI owns that step).
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
            semi.AddSlot(key);
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
                    ProgressionDestination.ForPopulation(semi.Id)));
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

        PlacePopulationEntriesIntoSlots(semi, destinationKeys, context.Clock);

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

        var competition = await CreateCompetitionFromRecipeAsync(
                context,
                recipe,
                cancellationToken,
                BootstrapRegulation.Standard())
            .ConfigureAwait(false);
        await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var roundOf32 = ConfigurePrimaryStage(context, competition, recipe);
        AssignRootComposition(roundOf32, [.. competition.Entries], context.Clock);
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

        MatchEnrichment.SpecializeWithExtraTimeAndPenalties(semi, context.Clock);
        MatchEnrichment.SpecializeWithExtraTimeAndPenalties(final, context.Clock);

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

        WireWinnerProgression(roundOf32, roundOf16, r32Fixtures, context.Clock);

        roundOf32.Prepare(context.Clock);
        competition.Prepare(context.Clock);
        roundOf32.Start(context.Clock);
        competition.Start(context.Clock);

        PlayDecisiveMatches(context, competition, r32Matches);
        ApplyAllProgressions(context, roundOf32, r32Fixtures, r32Matches, allStages);
        PlacePopulationEntriesIntoSlots(roundOf16, r16SlotKeys, context.Clock);

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
    /// World Cup: Groups 8×4 → Top2 → R16 population → Slot Draw → QF→SF → Final + Bronze;
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
            PlacesPerGroup = 4,
            StageName = "Phase de groupes",
            TeamNames = TeamNameSource.Dataset,
            DatasetCompetitionKey = "world-cup"
        };

        var competition = await CreateCompetitionFromRecipeAsync(
                context,
                recipe,
                cancellationToken,
                BootstrapRegulation.Standard())
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var groups = ConfigurePrimaryStage(context, competition, recipe);
        AssignRootComposition(groups, entries, context.Clock);

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

        MatchEnrichment.SpecializeWithExtraTimeAndPenalties(roundOf16, context.Clock);
        MatchEnrichment.SpecializeWithExtraTimeAndPenalties(quarter, context.Clock);
        MatchEnrichment.SpecializeWithExtraTimeAndPenalties(semi, context.Clock);
        MatchEnrichment.SpecializeWithExtraTimeAndPenalties(final, context.Clock);
        MatchEnrichment.SpecializeWithExtraTimeAndPenalties(bronze, context.Clock);

        WireWorldCupQualification(context.Ids, groups, roundOf16, context.Clock);

        var groupMatches = AssignThenMaterializeGroups(context, competition, groups, entries);
        PrepareAndStart(context, competition, groups);
        PlayMatches(context, competition, groupMatches, count: groupMatches.Count);

        var groupStandings = new Dictionary<GroupId, Standing>();
        foreach (var group in groups.Groups)
        {
            groupStandings[group.Id] = CalculateStanding.Execute(
                group.EntryIds,
                groupMatches,
                groups.Regulation.StandingRules ?? BootstrapRegulation.Standard().StandingRules);
        }

        Stage[] allStages = [groups, roundOf16, quarter, semi, final, bronze];
        ApplyQualification.Execute(
            groups,
            overallStanding: null,
            groupStandings,
            allStages,
            context.Clock);

        PlacePopulationIntoSlotsViaDraw(context, competition, roundOf16, r16SlotKeys);

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
        PlacePopulationEntriesIntoSlots(final, finalSlotKeys, context.Clock);
        PlacePopulationEntriesIntoSlots(bronze, bronzeSlotKeys, context.Clock);

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
    /// Hub Règlement QA seed: groupes classants + phase finale multi-tours (QF/SF A/R · Finale unique),
    /// ET+TAB MatchRules, remains <see cref="CompetitionStatus.Draft"/> so <c>ReplaceRegulation</c> stays available.
    /// </summary>
    public static async Task BuildRegulationHubDemoAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Démo Règlement",
            Format = RecipeFormat.Groups,
            TeamCount = 8,
            GroupCount = 2,
            PlacesPerGroup = 4,
            StageName = "Groupes",
            TeamNames = TeamNameSource.Generated
        };

        var baseline = BootstrapRegulation.Standard();
        var regulation = new Regulation(
            new EntryRules(minimumTeams: 8, maximumTeams: 16),
            baseline.MatchRules,
            baseline.StandingRules);

        var competition = await CreateCompetitionAsync(
                context,
                recipe.DisplayName,
                regulation: regulation,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(
                context,
                competition,
                recipe,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var groups = ConfigurePrimaryStage(context, competition, recipe);
        groups.ReplaceStandingRules(
            new StandingRules(
                new PointsPolicy(winPoints: 3, drawPoints: 1, lossPoints: 0),
                [
                    RankingCriterion.Points,
                    RankingCriterion.Wins,
                    RankingCriterion.GoalDifference,
                    RankingCriterion.GoalsFor,
                    RankingCriterion.HeadToHead
                ]),
            context.Clock);
        groups.ReplaceDrawRules(
            new DrawRules(DrawMode.Random, potRules: new PotRules(4)),
            context.Clock);
        AssignGroupsRoundRobin(groups, entries);
        _ = MaterializeGroupsMatches(context, competition, groups);

        var richTie = new TieFormat(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            awayGoalsRule: new AwayGoalsRule(),
            extraTimeRule: new ExtraTimeRule(),
            penaltyShootoutRule: new PenaltyShootoutRule());
        var finalTie = new TieFormat(TieFormat.SingleLeg, aggregateScoring: false);

        var knockout = Stage.Create(
            competition.Id,
            new StageName("Phase finale"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            context.Ids.Stage("final"),
            context.Clock);
        knockout.ReplaceDefaultTieFormat(richTie, context.Clock);
        var quarterRound = knockout.AddRound("Quarts de finale", richTie, context.Clock);
        var semiRound = knockout.AddRound("Demis de finale", richTie, context.Clock);
        var finalRound = knockout.AddRound("Finale", finalTie, context.Clock);
        knockout.ArrangeRounds([quarterRound.Id, semiRound.Id, finalRound.Id]);
        knockout.AddSlot("QF-1-A");
        knockout.AddSlot("QF-1-B");
        MatchEnrichment.SpecializeWithExtraTimeAndPenalties(knockout, context.Clock);
        competition.AddStage(knockout.Id, context.Clock);
        context.Stages.Add(knockout);

        var finalFixture = knockout.AddFixture(finalRound.Id, context.Clock);
        WireFinalPlacementAwards(knockout, finalFixture, context.Clock);

        WireEachGroupQualificationToPopulation(
            context.Ids, groups, knockout, positionFrom: 1, positionTo: 1, context.Clock);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Hub Règlement QA seed: single KO round with a rich TwoLegs TieFormat (homogeneous tokens),
    /// remains <see cref="CompetitionStatus.Draft"/>.
    /// </summary>
    public static async Task BuildRegulationTieHomogeneousDemoAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Démo Confrontation homogène",
            Format = RecipeFormat.Cup,
            TeamCount = 2,
            BracketSize = 2,
            StageName = "Finale",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var richTie = new TieFormat(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            awayGoalsRule: new AwayGoalsRule(),
            extraTimeRule: new ExtraTimeRule(),
            penaltyShootoutRule: new PenaltyShootoutRule());

        var final = CreateKnockoutStage(
            context,
            competition,
            "final",
            "Finale",
            "Finale",
            ["F-A", "F-B"]);
        MatchEnrichment.SpecializeWithExtraTimeAndPenalties(final, context.Clock);
        final.ReplaceDefaultTieFormat(richTie, context.Clock);
        final.ReplaceRoundTieFormat(final.Rounds[0].Id, richTie, context.Clock);
        var finalFixture = final.AddFixture(final.Rounds[0].Id, context.Clock);
        WireFinalPlacementAwards(final, finalFixture, context.Clock);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Structure / Confrontation QA: one Cup phase Draft with three rounds, each a distinct TieFormat.
    /// </summary>
    public static async Task BuildConfrontationMultiRoundDemoAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Démo Confrontation multi-tours",
            Format = RecipeFormat.Cup,
            TeamCount = 8,
            BracketSize = 8,
            StageName = "Tableau",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(
                context,
                competition,
                recipe,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Default ≠ any round — makes the « format par défaut » section visibly distinct.
        var defaultTie = new TieFormat(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            awayGoalsRule: new AwayGoalsRule());
        var quarterTie = new TieFormat(TieFormat.SingleLeg, aggregateScoring: false);
        var semiTie = new TieFormat(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            awayGoalsRule: new AwayGoalsRule(),
            extraTimeRule: new ExtraTimeRule());
        var finalTie = new TieFormat(
            TieFormat.SingleLeg,
            aggregateScoring: false,
            extraTimeRule: new ExtraTimeRule(),
            penaltyShootoutRule: new PenaltyShootoutRule());

        var stage = Stage.Create(
            competition.Id,
            new StageName("Tableau"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            context.Ids.Stage("tableau"),
            context.Clock);
        stage.ReplaceDefaultTieFormat(defaultTie, context.Clock);
        var quarter = stage.AddRound("Quarts de finale", quarterTie, context.Clock);
        var semi = stage.AddRound("Demis de finale", semiTie, context.Clock);
        var final = stage.AddRound("Finale", finalTie, context.Clock);
        stage.ArrangeRounds([quarter.Id, semi.Id, final.Id]);

        foreach (var key in PairSlotKeys("QF", pairCount: 4)
                     .Concat(PairSlotKeys("SF", pairCount: 2))
                     .Concat(["F-A", "F-B"]))
        {
            stage.AddSlot(key);
        }

        AssignRootComposition(stage, entries, context.Clock);
        competition.AddStage(stage.Id, context.Clock);
        context.Stages.Add(stage);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lightweight championship Draft seed for Règlement hub schematic QA.
    /// </summary>
    public static async Task BuildRegulationChampionshipDemoAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Démo Championnat",
            Format = RecipeFormat.Championship,
            TeamCount = 8,
            MatchdayCount = 14,
            MatchGenerationFormat = MatchGenerationFormat.DoubleRoundRobin,
            StageName = "Championnat",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var stage = ConfigurePrimaryStage(context, competition, recipe);
        _ = MaterializeChampionshipMatches(context, competition, stage);
        _ = entries;

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lightweight Swiss Draft seed for Règlement hub schematic QA (structure only, no rounds played).
    /// </summary>
    public static async Task BuildRegulationSwissDemoAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var recipe = new CompetitionRecipe
        {
            DisplayName = "Démo Suisse",
            Format = RecipeFormat.Swiss,
            TeamCount = 8,
            SwissRoundCount = 3,
            StageName = "Suisse",
            TeamNames = TeamNameSource.Generated
        };

        var competition = await CreateCompetitionFromRecipeAsync(context, recipe, cancellationToken)
            .ConfigureAwait(false);
        await RegisterTeamsAsync(context, competition, recipe, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        _ = ConfigurePrimaryStage(context, competition, recipe);

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
        WireWinnerProgression(stage, nextStage, fixtures, context.Clock);
        PrepareAndStartStage(context, stage);
        PlayDecisiveMatches(context, competition, matches);
        ApplyAllProgressions(context, stage, fixtures, matches, allStages);
        PlacePopulationEntriesIntoSlots(nextStage, nextSlotKeys, context.Clock);
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
    /// <item><c>prepared</c> — Running, 0 rounds (Overview ready for GenerateNextRound).</item>
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
        AssignGroupsRoundRobin(stage, entries);
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
        // A5: knockout / from-slots phases do not classify — no StandingRules seed.
        var regulation = StageRegulation.MaterializeFrom(
            competition.Regulation,
            isClassifyingPhase: false);
        var stage = Stage.Create(
            competition.Id,
            new StageName(stageName),
            regulation,
            context.Ids.Stage(stageKey),
            context.Clock);
        stage.AddRound(roundName, new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), context.Clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key);
        }

        competition.AddStage(stage.Id, context.Clock);
        context.Stages.Add(stage);
        return stage;
    }

    /// <summary>
    /// Qual V2: after ApplyQualification filled <see cref="Stage.CompositionEntries"/>,
    /// place them into form slots via a deterministic Slot Draw (then Publish + Apply).
    /// </summary>
    private static void PlacePopulationIntoSlotsViaDraw(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        string[] slotKeys)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(slotKeys);

        var pool = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();
        if (pool.Length == 0)
        {
            throw new InvalidOperationException(
                $"Stage '{stage.Name.Value}' has no population entries to place into slots.");
        }

        if (pool.Length != slotKeys.Length)
        {
            throw new InvalidOperationException(
                $"Population count ({pool.Length}) must match slot count ({slotKeys.Length}) for stage '{stage.Name.Value}'.");
        }

        var inputs = DrawInputsFactory.CreateDefault(competition, stage, DrawResolutionKind.Slot);
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, context.Ids.Draw(), context.Clock);
        stage.ConfigureDrawInputs(draw.Id, inputs);
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
                [.. pool.Select((entryId, index) => new SlotDrawPlacement(entryId, slotKeys[index]))]),
            context.Clock);
        stage.PublishDraw(draw.Id, context.Clock);
        ApplyDraw.Execute(stage, draw.Id, context.Clock);
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

    private static Fixture[] AddRoundFixtures(Stage stage, int count, IClock clock)
    {
        if (stage.Rounds.Count == 0)
        {
            throw new InvalidOperationException($"Stage '{stage.Name.Value}' has no rounds for fixtures.");
        }

        var roundId = stage.Rounds[0].Id;
        var fixtures = new Fixture[count];
        for (var i = 0; i < count; i++)
        {
            fixtures[i] = stage.AddFixture(roundId, clock);
        }

        return fixtures;
    }

    private static void WireWorldCupQualification(
        DeterministicIdFactory ids,
        Stage groups,
        Stage roundOf16,
        IClock clock) =>
        WireEachGroupQualificationToPopulation(
            ids, groups, roundOf16, positionFrom: 1, positionTo: 2, clock, intentKey: "qual-wc-top2");

    /// <summary>
    /// Qual V2 authoring: EachGroup positions → destination stage population (Intents SoT).
    /// </summary>
    private static void WireEachGroupQualificationToPopulation(
        DeterministicIdFactory ids,
        Stage source,
        Stage destination,
        int positionFrom,
        int positionTo,
        IClock clock,
        string intentKey = "qual-each")
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(clock);

        var groupOrder = source.Groups
            .OrderBy(group => group.Name, StringComparer.Ordinal)
            .Select(group => group.Id)
            .ToArray();
        if (groupOrder.Length == 0)
        {
            throw new InvalidOperationException(
                $"Stage '{source.Name.Value}' has no groups for EachGroup qualification.");
        }

        source.ReplaceQualificationRules(
            QualificationRules.FromIntents(
            [
                new QualificationIntent(
                    ids.Intent(intentKey),
                    order: 1,
                    QualificationIntentSourceKind.EachGroup,
                    positionFrom,
                    positionTo,
                    destination.Id)
            ],
            groupOrder),
            clock);
    }

    private static void WireWinnerProgression(
        Stage source,
        Stage destination,
        Fixture[] fixtures,
        IClock clock)
    {
        var paths = new List<ProgressionPath>(fixtures.Length);
        paths.AddRange(fixtures.Select(t => new ProgressionPath(
            t.Id,
            ProgressionOutcome.Winner,
            ProgressionDestination.ForPopulation(destination.Id))));

        source.ReplaceProgressionRules(new ProgressionRules(paths), clock);
    }

    /// <summary>
    /// After Prog → Population (V3), place resolved entries into form slots for MaterializeFromSlots.
    /// Scenario orchestration only — not a Domain Progression destination.
    /// </summary>
    private static void PlacePopulationEntriesIntoSlots(
        Stage stage,
        string[] slotKeys,
        IClock clock)
    {
        var entries = stage.CompositionEntries
            .Select(e => e.EntryId)
            .ToArray();
        if (entries.Length < slotKeys.Length)
        {
            throw new InvalidOperationException(
                $"Stage '{stage.Name.Value}' has {entries.Length} population entries but {slotKeys.Length} slots to fill.");
        }

        for (var i = 0; i < slotKeys.Length; i++)
        {
            stage.ApplyResolvedEntry(slotKeys[i], entries[i], clock);
        }
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

        // V3: inter-phase = Population only (no ForSlot other stage).
        var paths = new ProgressionPath[]
        {
            new(sfFixtures[0].Id, ProgressionOutcome.Winner, ProgressionDestination.ForPopulation(final.Id)),
            new(sfFixtures[1].Id, ProgressionOutcome.Winner, ProgressionDestination.ForPopulation(final.Id)),
            new(sfFixtures[0].Id, ProgressionOutcome.Loser, ProgressionDestination.ForPopulation(bronze.Id)),
            new(sfFixtures[1].Id, ProgressionOutcome.Loser, ProgressionDestination.ForPopulation(bronze.Id))
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

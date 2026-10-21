// -----------------------------------------------------------------------
// <copyright file="CupMaterializeRecetteProtocolTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// Executable recette protocol — Cup materialize contract (domain, read-model, command).
/// Maps to R1–R8 in structure-lifecycle Cup recette checklist.
/// </summary>
[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
public sealed class CupMaterializeRecetteProtocolTests(DevelopmentPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 18, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task R2_blue_pair_materializes_with_coherent_match_countAsync()
    {
        await using var scope = await OpenRecetteCupScopeAsync("RECETTE C1");
        var (competition, stage, hub, matches) = scope;
        hub.Status.Should().Be(StageStatus.Draft);
        EnsureRecetteCupReady(competition, stage);

        var hubReady = AssembleHub(competition, [stage], matches);
        var row = hubReady.Materialize!.Confrontations.Single(r => r.Eligible);
        row.Determined.Should().BeTrue();
        hubReady.Materialize!.CommandEnabled.Should().BeTrue();

        var result = MaterializeCupFromOccupiedSlots.Execute(competition, stage, null, matches, _clock);
        result.AlreadyComplete.Should().BeFalse();
        result.CreatedMatches.Should().HaveCount(1, "recette C1 uses SingleLeg tie format");

        var fixtureByBracketPairKey = stage.FindFixtureByBracketPairKey(row.Key);
        fixtureByBracketPairKey.Should().NotBeNull();
        fixtureByBracketPairKey.Attachments.Should().ContainSingle();
    }

    [Fact]
    public async Task R3_idempotence_and_already_complete_are_distinctAsync()
    {
        await using var scope = await OpenRecetteCupScopeAsync("RECETTE C3");
        var (competition, stage, _, _) = scope;
        EnsureRecetteCupReady(competition, stage);
        var matchList = await scope.ReloadMatchesAsync();

        var hub = AssembleHub(competition, [stage], matchList);
        hub.Materialize!.MaterializedCount.Should().Be(hub.Materialize.DeterminedCount);
        hub.Materialize.CommandEnabled.Should().BeFalse();

        var second = MaterializeCupFromOccupiedSlots.Execute(competition, stage, null, matchList, _clock);
        second.CreatedMatches.Should().BeEmpty();
        second.AlreadyComplete.Should().BeTrue();
    }

    [Fact]
    public async Task R1_grey_pair_prog_rule_without_entry_not_determinedAsync()
    {
        await using var scope = await OpenRecetteCupScopeAsync("RECETTE C1");
        var (competition, stage, _, matches) = scope;
        EnsureRecetteCupReady(competition, stage);
        var hubReady = AssembleHub(competition, [stage], matches);

        var undetermined = hubReady.Materialize!.Confrontations
            .Where(row => !row.Determined)
            .ToArray();
        undetermined.Should().NotBeEmpty("C1 places only one of four bracket pairs");

        foreach (var row in undetermined)
        {
            var pair = stage.FindBracketPair(row.Key);
            pair.Should().NotBeNull();
            var slotA = stage.FindSlot(pair.SlotAKey);
            var slotB = stage.FindSlot(pair.SlotBKey);
            (slotA?.EntryId is null || slotB?.EntryId is null).Should().BeTrue();
        }
    }

    [Fact]
    public Task R6_cup_draw_obsolete_read_and_command_align()
    {
        try
        {
            var competition = CreateCompetition.Execute("recette-r6-cup", _clock);
            var stage = ConfigureStructure.Execute(
                competition,
                null,
                StructureIntent.Cup(4),
                _clock).Stage;
            stage.AddSlot("SF1-A");
            stage.AddSlot("SF1-B");
            ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
            var ids = Enumerable.Range(0, 2)
                .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
                .ToArray();
            stage.ReplaceAffectationAuthoring(ids, _clock);
            var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
            ConfigureDrawInputs.Execute(stage, draw.Id, DrawInputs.ForSlot(ids));
            stage.RecordDrawResolution(
                draw.Id,
                DrawResolution.ResolvedSlots(
                [
                    new SlotDrawPlacement(ids[0], "SF1-A"),
                    new SlotDrawPlacement(ids[1], "SF1-B")
                ]),
                _clock);
            PublishDraw.Execute(stage, draw.Id, _clock);
            stage.SeedEntryRoundBracketPairs();
            stage.ApplyResolvedEntry("SF1-A", ids[0], _clock);
            stage.ApplyResolvedEntry("SF1-B", EntryId.New(), _clock);
            PrepareStage.Execute(stage, [stage], _clock);
            stage.ReplaceAffectationAuthoringOperationally([ids[0]], _clock);
            DrawConsumptionConformance.IsObsoleteForConsumption(stage.GetDraw(draw.Id), stage).Should().BeTrue();

            var hub = AssembleHub(competition, [stage], []);
            hub.Materialize.Should().NotBeNull();
            hub.Materialize.CommandEnabled.Should().BeFalse();
            hub.Materialize.DisabledReason.Should().Be(StructureMaterializeDisabledReasons.DrawObsoleteForConsumption);

            var act = () => MaterializeCupFromOccupiedSlots.Execute(competition, stage, ["P1"], [], _clock);
            act.Should().Throw<ApplicationFailureException>()
                .Which.Code.Should().Be(ApplicationErrorCodes.DrawObsoleteForConsumption);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    [Fact]
    public async Task R7_prog_filled_slots_have_entry_id_on_semiAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("cup-qf-sf")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var competition = await competitions.GetByIdForUpdateAsync(
            (await competitions.ListAsync()).Single().Id);
        var allStages = await stages.GetByIdsForUpdateAsync(competition!.StageIds);
        var semi = allStages[1];

        semi.Slots.Should().OnlyContain(slot => slot.EntryId != null);
        var schematic = StageSchematicAssembler.Assemble(semi, competition, allStages);
        foreach (var c in schematic.Cases)
        {
            c.FeedOrigin.Should().NotBeNull();
            c.FeedOrigin!.Kind.Should().Be(FeedKind.Progression);
            c.Entry.Should().NotBeNull();
        }
    }

    [Fact]
    public void R5a_p2_materializes_when_p1_award_unreachable()
    {
        var competition = CreateCompetition.Execute("recette-r5a", _clock);
        var stage = CreateSemiStage(competition);
        competition.AddStage(stage.Id, _clock);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules([new PlacementAwardPath("P1", ProgressionOutcome.Winner, rank: 1)]),
            _clock);
        stage.ReplaceBracketPairs([new BracketPair("P2", "SF2-A", "SF2-B")]);
        stage.ApplyResolvedEntry("SF2-A", EntryId.New(), _clock);
        stage.ApplyResolvedEntry("SF2-B", EntryId.New(), _clock);
        stage.Prepare(_clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(competition, stage, null, [], _clock);
        result.CreatedMatches.Should().HaveCount(1);
        result.AlreadyComplete.Should().BeFalse();
    }

    [Fact]
    public async Task R4_exec001_development_seed_recette_snapshotAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("invariants-exec-undecidable")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var competition = await competitions.GetByIdForUpdateAsync(
            (await competitions.ListAsync()).Single().Id);
        var allStages = await stages.GetByIdsForUpdateAsync(competition!.StageIds);
        var semi = allStages.First(s => s.Name.Value.Contains("Demi", StringComparison.Ordinal));
        var final = allStages.First(s => s.Name.Value.Contains("Finale", StringComparison.Ordinal));

        semi.Status.Should().Be(StageStatus.Draft);
        CompetitionFlowAnalyzer.AnalyzeExecution([semi])
            .Should()
            .NotContain(
                diagnostic => diagnostic.Code == CompetitionFlowAnalyzer.ConfrontationUndecidable,
                "EXEC-001 Dev seed: undecidable not projected on semi alone (see g-mat1 doc vs orchestration)");

        var view = StructureViewAssembler.Assemble(competition, allStages);
        view.Stages.Single(s => s.StageId == final.Id.Value).StructureIssues
            .Should()
            .NotContain(
                CompetitionFlowAnalyzer.ConfrontationUndecidable,
                "finale hub: progression-fed places, not undecidable at structure read");

        var prepareSemi = () => PrepareStage.Execute(semi, allStages, _clock);
        prepareSemi.Should().NotThrow();
        semi.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void R5b_harness_gmat2_refuses_when_eligible_intersects_unreachable()
    {
        var competition = CreateCompetition.Execute("recette-r5b", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Finale"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(stage.Id, _clock);
        var tie = new TieFormat(
            TieFormat.SingleLeg,
            aggregateScoring: false,
            penaltyShootoutRule: new PenaltyShootoutRule());
        var round = stage.AddRound("F", tie, _clock);
        stage.AddFixture(round.Id, _clock);
        stage.AddSlot("F-A");
        stage.AddSlot("F-B");
        stage.ReplaceBracketPairs([new BracketPair("FINAL", "F-A", "F-B")]);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules([new PlacementAwardPath("FINAL", ProgressionOutcome.Winner, rank: 1)]),
            _clock);
        stage.ClearBracketPairs();
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ReplaceAffectationAuthoring([a, b], _clock);
        stage.AssignEntryToSlot("F-A", a);
        stage.AssignEntryToSlot("F-B", b);
        stage.Prepare(_clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(competition, stage, ["FINAL"], [], _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AttributionUnreachable);
    }

    [Fact]
    public async Task R8_groups_running_not_mutable_not_same_as_unresolvedAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("structure-materialize-recette")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();

        var summary = (await competitions.ListAsync())
            .Should()
            .ContainSingle(c => c.Name.Value.StartsWith("RECETTE R1", StringComparison.Ordinal))
            .Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        var stage = (await stages.GetByIdsForUpdateAsync(competition!.StageIds)).Single();
        stage.Status.Should().Be(StageStatus.Running);

        var matchList = await matches.ListByStageForUpdateAsync(stage.Id);
        var hub = AssembleHub(competition, [stage], matchList);
        hub.Materialize!.CommandEnabled.Should().BeFalse();
        hub.Materialize.DisabledReason.Should().Be(StructureMaterializeDisabledReasons.StructureNotMutable);
        hub.Materialize.DeterminedCount.Should().BeGreaterThan(hub.Materialize.MaterializedCount);
        hub.StructureIssues.Should().NotContain(CompetitionFlowAnalyzer.PopulationShortfall);
    }

    private Stage CreateSemiStage(Competition competition)
    {
        var regulation = StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true);
        var stage = Stage.Create(competition.Id, new StageName("SF"), regulation, _clock);
        stage.AddRound("Semi-Finals", tieFormat: null, _clock);
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        stage.AddSlot("SF2-A");
        stage.AddSlot("SF2-B");
        stage.SeedEntryRoundBracketPairs();
        return stage;
    }

    private void EnsureRecetteCupReady(Competition competition, Stage stage)
    {
        StageMaterializationTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);
        PrepareCompetition.Execute(competition, _clock);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    private async Task<RecetteCupScope> OpenRecetteCupScopeAsync(string namePrefix)
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("structure-materialize-recette")]);

        var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();

        var summary = (await competitions.ListAsync())
            .Should()
            .ContainSingle(c => c.Name.Value.StartsWith(namePrefix, StringComparison.Ordinal))
            .Subject;
        var competition = (await competitions.GetByIdForUpdateAsync(summary.Id))!;
        var stage = (await stages.GetByIdsForUpdateAsync(competition.StageIds)).Single();
        var matchList = await matches.ListByStageForUpdateAsync(stage.Id);
        var hub = AssembleHub(competition, [stage], matchList);
        return new RecetteCupScope(scope, competition, stage, hub, matchList, matches);
    }

    private sealed class RecetteCupScope(
        IServiceScope scope,
        Competition competition,
        Stage stage,
        StructureStageHubSummaryDto hub,
        IReadOnlyList<Match> matches,
        IMatchRepository matchRepository)
        : IAsyncDisposable
    {
        private readonly StageId _stageId = stage.Id;

        public Competition Competition { get; } = competition;
        public Stage Stage { get; } = stage;
        public StructureStageHubSummaryDto Hub { get; } = hub;
        public IReadOnlyList<Match> Matches { get; } = matches;

        public void Deconstruct(
            out Competition competition,
            out Stage stage,
            out StructureStageHubSummaryDto hub,
            out IReadOnlyList<Match> matches)
        {
            competition = Competition;
            stage = Stage;
            hub = Hub;
            matches = Matches;
        }

        public async Task<IReadOnlyList<Match>> ReloadMatchesAsync() =>
            await matchRepository.ListByStageForUpdateAsync(_stageId);

        public ValueTask DisposeAsync()
        {
            scope.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private static StructureStageHubSummaryDto AssembleHub(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches)
    {
        var stage = stages[0];
        var rows = matches
            .Select(match => new MatchSummaryRow(
                match.Id,
                match.StageId,
                match.Status,
                match.HomeEntryId,
                match.AwayEntryId,
                match.Result,
                match.HasObservedLive))
            .ToArray();
        var view = StructureViewAssembler.Assemble(
            competition,
            stages,
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
            {
                [stage.Id] = rows
            });
        return view.Stages.Single(s => s.StageId == stage.Id.Value);
    }
}

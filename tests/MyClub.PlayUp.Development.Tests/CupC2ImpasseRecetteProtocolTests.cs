// -----------------------------------------------------------------------
// <copyright file="CupC2ImpasseRecetteProtocolTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// Executable recette C2 — Start / D2 / recovery chain (Notion Cup arbitration).
/// Maps to checklist track A (impasse): A3 at 1/2, A4 materialize, A5 Start blocked (1a).
/// Cas assign (manual slot before Start @ impasse 1/2) hors checklist A2 — arbitrage AUD-0004 : à qualifier (p.ex. piste B4), pas couvert ici.
/// </summary>
[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
public sealed class CupC2ImpasseRecetteProtocolTests(DevelopmentPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 17, 19, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task C2_manual_recette_chain_documents_backend_and_read_modelAsync()
    {
        var scope = await OpenRecetteCupScopeAsync("RECETTE C2");
        await using var _ = scope;
        var (competition, stage, hub, matches) = scope;
        EnsureRecetteCupReady(competition, stage);

        matches.Should().NotBeNull();
        hub.Should().NotBeNull();

        matches = await scope.ReloadMatchesAsync();
        hub = AssembleHub(competition, [stage], matches);

        hub.Status.Should().Be(StageStatus.Ready);
        hub.Materialize!.MaterializedCount.Should().Be(1);
        hub.Materialize.DeterminedCount.Should().Be(2);
        hub.Materialize.EligibleCount.Should().Be(1);

        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();

        // A3 @ 1/2 without assign — backend guard/read: IncompleteEntryRound (checklist SPA may still say Unmaterialized).
        hub.Lifecycle!.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledIncompleteEntryRound);

        var startAct = async () => await executor.StartStageAsync(stage.Id);
        (await startAct.Should().ThrowAsync<ApplicationFailureException>())
            .Which.Code.Should().Be(ApplicationErrorCodes.StartBlockedIncompleteEntryRound);

        var materializeSecond = MaterializeCupFromOccupiedSlots.Execute(competition, stage, null, matches, _clock);
        materializeSecond.CreatedMatches.Should().HaveCount(1);
        foreach (var created in materializeSecond.CreatedMatches)
        {
            scope.MatchRepository.Add(created);
        }

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.SaveChangesAsync();
        matches = await scope.ReloadMatchesAsync();
        hub = AssembleHub(competition, [stage], matches);

        hub.Materialize!.MaterializedCount.Should().Be(2);
        hub.Materialize.DeterminedCount.Should().Be(2);
        hub.Materialize.EligibleCount.Should().Be(0);
        hub.Materialize.Confrontations.Count(row => !row.Determined).Should().Be(2);

        hub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledIncompleteEntryRound);

        var startAfterMatAct = async () => await executor.StartStageAsync(stage.Id);
        (await startAfterMatAct.Should().ThrowAsync<ApplicationFailureException>())
            .Which.Code.Should().Be(ApplicationErrorCodes.StartBlockedIncompleteEntryRound);
        stage.Status.Should().Be(StageStatus.Ready);
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
        IMatchRepository matchRepository) : IAsyncDisposable
    {
        private readonly StageId _stageId = stage.Id;

        public Competition Competition { get; } = competition;
        public Stage Stage { get; } = stage;
        public StructureStageHubSummaryDto Hub { get; } = hub;
        public IReadOnlyList<Match> Matches { get; } = matches;
        public IServiceProvider ServiceProvider { get; } = scope.ServiceProvider;
        public IMatchRepository MatchRepository => matchRepository;

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
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>> { [stage.Id] = rows });
        return view.Stages.Single(s => s.StageId == stage.Id.Value);
    }
}

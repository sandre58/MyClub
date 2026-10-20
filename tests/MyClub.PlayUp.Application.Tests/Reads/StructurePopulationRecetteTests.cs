// -----------------------------------------------------------------------
// <copyright file="StructurePopulationRecetteTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Reference recette for Structure population diagnostics (B/C) — observation only, no rule changes.
/// Set UPDATE_POPULATION_RECETTE_FIXTURES=1 to refresh JSON under web/__fixtures__/populationRecette.
/// </summary>
public sealed class StructurePopulationRecetteTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Running_root_championship_8places_8composition_1withdrawn_U1_satisfied()
    {
        var situation = BuildRunningRootEightCompositionOneWithdrawn();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Status.Should().Be(StageStatus.Running);
        hub.IsRootComposition.Should().BeTrue();
        var u1 = StructurePopulationVolume.ExpectedOccupancy(view, hub);

        AssertOccupationEligibilityWitness(
            view,
            hub,
            situation,
            expectPopulationShortfall: false);

        AssertCommittedFixture(
            "running-root-u1-satisfied.json",
            Observe(
                view,
                hub,
                u1,
                inboundConfiguredVolume: 0,
                isRosterComplete: ChampionshipSwissPhasePlaces.IsRosterComplete(
                    situation.Competition,
                    situation.RequirePrimaryStage(),
                    situation.Stages)));
    }

    /// <summary>
    /// Domain: forfait requires competition Running — cannot observe 7 eligible with 8 affectation
    /// + 7 composition on Ready (affectation replace syncs composition). This Ready recette uses
    /// 7 affectation seats (7 composition) on 8 Places N to assert eligible-roster gating before start.
    /// </summary>
    [Fact]
    public void Ready_root_8places_7affectation_seats_PhaseRosterIncomplete()
    {
        var situation = BuildReadyRootEightPlacesSevenAffectationSeats();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Status.Should().Be(StageStatus.Ready);
        hub.AffectationEntryCount.Should().Be(7);
        hub.CompositionEntryCount.Should().Be(7);
        hub.CompositionCapacity.Should().Be(8);
        hub.CompositionIneligibleCount.Should().Be(0);
        view.Participants.ActiveCount.Should().Be(8);
        view.Participants.OccupyingCount.Should().Be(8);

        var u1 = StructurePopulationVolume.ExpectedOccupancy(view, hub);
        var places = StructurePopulationVolume.ResolvePlacesN(hub);
        u1.Should().Be(7);
        places.Should().Be(8);
        (u1 - places).Should().Be(-1);

        hub.StructureIssues.Should().Contain(StructureViewAssembler.IssueSlotFeedMissing);
        hub.StructureIssues.Should().NotContain(CompetitionFlowAnalyzer.PopulationShortfall);

        hub.PresentedIssueCodes.Should().Contain(CompetitionFlowAnalyzer.PopulationShortfall);
        hub.PresentedIssueCodes.Should().Contain(
            StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete);
        CodesOnlyInPresentation(hub).Should().Contain(CompetitionFlowAnalyzer.PopulationShortfall);

        hub.Materialize.Should().NotBeNull();
        hub.Materialize!.DisabledReason.Should().Be(
            StructureMaterializeDisabledReasons.PhaseRosterIncomplete);

        ChampionshipSwissPhasePlaces.IsRosterComplete(
            situation.Competition,
            situation.RequirePrimaryStage(),
            situation.Stages).Should().BeFalse();

        AssertPresentationParity(view, hub);

        AssertCommittedFixture(
            "ready-root-seven-affectation-seats.json",
            Observe(
                view,
                hub,
                u1,
                inboundConfiguredVolume: 0,
                isRosterComplete: false));
    }

    [Fact]
    public void Running_root_U1_deficit_despite_full_composition()
    {
        var situation = BuildRunningRootU1DeficitFullComposition();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.AffectationEntryCount.Should().Be(7);
        hub.CompositionEntryCount.Should().Be(8);
        hub.CompositionCapacity.Should().Be(8);

        var u1 = StructurePopulationVolume.ExpectedOccupancy(view, hub);
        var places = StructurePopulationVolume.ResolvePlacesN(hub);
        u1.Should().Be(7);
        places.Should().Be(8);
        (u1 - places).Should().Be(-1);
        hub.CompositionEntryCount.Should().Be(places);

        hub.StructureIssues.Should().NotContain(CompetitionFlowAnalyzer.PopulationShortfall);
        hub.PresentedIssueCodes.Should().Contain(CompetitionFlowAnalyzer.PopulationShortfall);
        CodesOnlyInPresentation(hub).Should().Equal([CompetitionFlowAnalyzer.PopulationShortfall]);
        StructurePopulationVolume.ShouldSynthesizePopulationShortfall(
            view,
            hub,
            hub.StructureIssues ?? []).Should().BeTrue();

        AssertPresentationParity(view, hub);

        AssertCommittedFixture(
            "running-root-u1-deficit-full-composition.json",
            Observe(
                view,
                hub,
                u1,
                inboundConfiguredVolume: 0,
                isRosterComplete: ChampionshipSwissPhasePlaces.IsRosterComplete(
                    situation.Competition,
                    situation.RequirePrimaryStage(),
                    situation.Stages)));
    }

    private TestCompetition BuildRunningRootEightCompositionOneWithdrawn()
    {
        var situation = TestCompetition.Create("Recette Running 8+1W", _clock)
            .WithTeams(8)
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        var ids = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ReplaceAffectationAuthoring(ids, _clock);
        situation.PreparePrimaryStage();
        situation.PrepareCompetition();
        situation.StartCompetition();
        situation.StartPrimaryStage();

        var withdrawn = ids.OrderBy(id => id.Value).First();
        WithdrawEntry.Execute(situation.Competition, withdrawn, situation.Stages, [], _clock);
        return situation;
    }

    private TestCompetition BuildReadyRootEightPlacesSevenAffectationSeats()
    {
        var situation = TestCompetition.Create("Recette Ready 7 seats", _clock)
            .WithTeams(8)
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        var ids = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ReplaceAffectationAuthoring(ids.Take(7).ToArray(), _clock);
        PrepareStage.Execute(stage, situation.Stages, _clock);
        return situation;
    }

    private TestCompetition BuildRunningRootU1DeficitFullComposition()
    {
        var situation = TestCompetition.Create("Recette U1 deficit", _clock)
            .WithTeams(8)
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        var ids = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ReplaceAffectationAuthoring(ids.Take(7).ToArray(), _clock);
        ChampionshipSwissPhasePlaces.FillCompositionToPlacesCapacity(
            situation.Competition,
            stage,
            situation.Stages,
            _clock);
        PrepareStage.Execute(stage, situation.Stages, _clock);
        situation.PrepareCompetition();
        situation.StartCompetition();
        situation.StartPrimaryStage();
        return situation;
    }

    private static void AssertOccupationEligibilityWitness(
        StructureViewDto view,
        StructureStageHubSummaryDto hub,
        TestCompetition situation,
        bool expectPopulationShortfall)
    {
        var places = StructurePopulationVolume.ResolvePlacesN(hub);
        var u1 = StructurePopulationVolume.ExpectedOccupancy(view, hub);
        places.Should().Be(8);
        u1.Should().Be(places);
        view.Participants.OccupyingCount.Should().Be(places!.Value);
        view.Participants.ActiveCount.Should().Be(7);
        (view.Participants.OccupyingCount - view.Participants.ActiveCount).Should().Be(1);
        hub.CompositionEntryCount.Should().Be(places);
        hub.CompositionIneligibleCount.Should().Be(1);

        hub.StructureIssues.Should().NotContain(CompetitionFlowAnalyzer.PopulationShortfall);
        hub.PresentedIssueCodes.Should().NotContain(CompetitionFlowAnalyzer.PopulationShortfall);
        if (expectPopulationShortfall)
        {
            throw new InvalidOperationException("Witness scenario must not expect PopulationShortfall.");
        }

        ChampionshipSwissPhasePlaces.IsRosterComplete(
            situation.Competition,
            situation.RequirePrimaryStage(),
            situation.Stages).Should().BeFalse();

        AssertPresentationParity(view, hub);
    }

    private static void AssertPresentationParity(StructureViewDto view, StructureStageHubSummaryDto hub)
    {
        var computed = StructurePresentedStageDiagnostics.CollectPresentedIssueCodes(view, hub);
        hub.PresentedIssueCodes.Should().Equal(computed);
    }

    private static string[] CodesOnlyInPresentation(StructureStageHubSummaryDto hub)
    {
        var raw = hub.StructureIssues?.ToHashSet(StringComparer.Ordinal) ?? [];
        return hub.PresentedIssueCodes?
            .Where(code => !raw.Contains(code))
            .ToArray() ?? [];
    }

    private static PopulationRecetteObservationDto Observe(
        StructureViewDto view,
        StructureStageHubSummaryDto hub,
        int expectedPopulationOccupancy,
        int inboundConfiguredVolume,
        bool isRosterComplete)
    {
        var places = StructurePopulationVolume.ResolvePlacesN(hub) ?? 0;
        var raw = hub.StructureIssues?.ToArray() ?? [];
        var presented = hub.PresentedIssueCodes?.ToArray() ?? [];
        return new PopulationRecetteObservationDto(
            hub.StageId,
            hub.Status.ToString(),
            hub.AffectationEntryCount,
            hub.CompositionEntryCount,
            hub.CompositionCapacity,
            hub.CompositionIneligibleCount,
            expectedPopulationOccupancy,
            inboundConfiguredVolume,
            places,
            expectedPopulationOccupancy - places,
            view.Participants.ActiveCount,
            view.Participants.OccupyingCount,
            view.Participants.OccupyingCount - view.Participants.ActiveCount,
            isRosterComplete,
            raw,
            presented,
            CodesOnlyInPresentation(hub),
            hub.Materialize?.DisabledReason,
            hub.Lifecycle?.Transition.DisabledReasonCode);
    }

    private void AssertCommittedFixture(
        string fileName,
        PopulationRecetteObservationDto observed)
    {
        var path = FixturePath(fileName);
        if (Environment.GetEnvironmentVariable("UPDATE_POPULATION_RECETTE_FIXTURES") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(observed, JsonOptions));
            return;
        }

        File.Exists(path).Should().BeTrue($"fixture missing: {path}");
        var expected = JsonSerializer.Deserialize<PopulationRecetteObservationDto>(
            File.ReadAllText(path),
            JsonOptions);
        observed.Should().BeEquivalentTo(
            expected,
            options => options.Excluding(dto => dto.StageId));
    }

    private static string FixturePath(string fileName) =>
        Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "PlayUp",
                "web",
                "src",
                "pages",
                "structure",
                "__fixtures__",
                "populationRecette",
                fileName));

    private sealed record PopulationRecetteObservationDto(
        Guid StageId,
        string StageStatus,
        int AffectationEntryCount,
        int CompositionEntryCount,
        int? CompositionCapacity,
        int CompositionIneligibleCount,
        int ExpectedPopulationOccupancy,
        int InboundConfiguredVolume,
        int PlacesN,
        int U1PopulationGap,
        int ParticipantsActiveCount,
        int ParticipantsOccupyingCount,
        int WithdrawnEntryCount,
        bool IsRosterComplete,
        string[] StructureIssues,
        string[] PresentedIssueCodes,
        string[] PresentedOnlyIssueCodes,
        string? MaterializeDisabledReason,
        string? LifecycleTransitionDisabledReasonCode);
}

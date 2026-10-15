// -----------------------------------------------------------------------
// <copyright file="StructureDiagnosticPresentationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureDiagnosticPresentationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ResolveSeverity_PopulationShortfall_blocking_in_Ready_R_Pres_PS_2()
    {
        var (view, hub) = GroupsStageWithShortfall();
        var readyHub = hub with { Status = StageStatus.Ready };

        StructureDiagnosticPresentation.ResolveSeverity(
                CompetitionFlowAnalyzer.PopulationShortfall,
                readyHub,
                view)
            .Should().Be(StructureDiagnosticPresentation.PresentationSeverity.Blocking);
    }

    [Fact]
    public void ResolveSeverity_PopulationShortfall_warning_in_Draft()
    {
        var (view, hub) = GroupsStageWithShortfall();

        StructureDiagnosticPresentation.ResolveSeverity(
                CompetitionFlowAnalyzer.PopulationShortfall,
                hub,
                view)
            .Should().Be(StructureDiagnosticPresentation.PresentationSeverity.Warning);
    }

    [Fact]
    public void ResolveSeverity_OverCapacityEdge_always_warning()
    {
        var (view, hub) = GroupsStageWithShortfall();
        hub = hub with { Status = StageStatus.Ready };

        StructureDiagnosticPresentation.ResolveSeverity(
                CompetitionFlowAnalyzer.OverCapacityEdge,
                hub,
                view)
            .Should().Be(StructureDiagnosticPresentation.PresentationSeverity.Warning);
    }

    [Fact]
    public void ResolvePhaseBucket_Suspended_maps_to_Running()
    {
        StructureDiagnosticPresentation.ResolvePhaseBucket(StageStatus.Suspended)
            .Should().Be(StructureDiagnosticPresentation.PresentationPhaseBucket.Running);
    }

    [Fact]
    public void Collect_PopulationShortfall_blocking_when_stage_Ready()
    {
        var (view, hub) = GroupsStageWithShortfall();
        var readyView = view with
        {
            Stages = [hub with { Status = StageStatus.Ready }]
        };
        var items = new List<NeedsAttentionItemDto>();
        StructurePresentedAttentionCollector.Collect(readyView, items);

        items.Should().ContainSingle(item =>
            item.Source == CompetitionFlowAnalyzer.PopulationShortfall
            && item.Severity == NeedsAttentionAssembler.SeverityBlocking);
    }

    private (StructureViewDto View, StructureStageHubSummaryDto Hub) GroupsStageWithShortfall()
    {
        var competition = CreateCompetition.Execute("Pres", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);

        var poules = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(poules.Id, _clock);
        poules.AddGroup("A", _clock);
        poules.AddGroup("B", _clock);
        poules.SetPlacesPerGroup(4);
        poules.AddMatchday(1, _clock);

        var view = StructureViewAssembler.Assemble(competition, [poules]);
        var hub = view.Stages.Single();
        hub.Status.Should().Be(StageStatus.Draft);
        return (view, hub);
    }
}

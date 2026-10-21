// -----------------------------------------------------------------------
// <copyright file="StructureViewAssemblerHubNextStepTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Read-model integration: <see cref="StructureHubNextStepResolver"/> projected on <see cref="StructureViewDto.HubNextStep"/>.
/// </summary>
public sealed class StructureViewAssemblerHubNextStepTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 18, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_projects_prepare_on_single_draft_phase()
    {
        var situation = TestCompetition.Create("Hub prepare", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"));

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);

        view.HubNextStep.Should().NotBeNull();
        view.HubNextStep!.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        view.HubNextStep.StageName.Should().Be("League");
        view.HubNextStep.StageId.Should().Be(situation.PrimaryStage!.Id.Value);
    }

    [Fact]
    public void Assemble_projects_null_when_two_phases_share_minimum_prepare_tier()
    {
        var competition = Competition.Create(new CompetitionName("Two draft"), SampleRegulations.Standard(), _clock);
        foreach (var label in new[] { "A", "B", "C", "D" })
        {
            AddEntry.Execute(competition, label, _clock);
        }

        var stageA = Stage.Create(competition.Id, new StageName("Phase A"), SampleRegulations.Standard(), _clock);
        stageA.AddMatchday(1, _clock);
        competition.AddStage(stageA.Id, _clock);
        foreach (var entry in competition.Entries)
        {
            stageA.AddResolvedPopulationEntry(entry.Id, _clock);
        }

        var stageB = Stage.Create(competition.Id, new StageName("Phase B"), SampleRegulations.Standard(), _clock);
        stageB.AddMatchday(1, _clock);
        competition.AddStage(stageB.Id, _clock);
        foreach (var entry in competition.Entries)
        {
            stageB.AddResolvedPopulationEntry(entry.Id, _clock);
        }

        var view = StructureViewAssembler.Assemble(competition, [stageA, stageB]);

        view.HubNextStep.Should().BeNull();
    }

    [Fact]
    public void Assemble_projects_run_draw_when_gate_allows_not_badge_only()
    {
        var situation = TestCompetition.Create("Hub run draw", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(
            [.. situation.Competition.Entries.Select(entry => entry.Id)],
            _clock);
        PrepareStage.Execute(stage, situation.Stages, _clock);

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);

        view.HubNextStep.Should().NotBeNull();
        view.HubNextStep!.Code.Should().Be(StructureViewAssembler.NextStepRunDraw);
    }

    [Fact]
    public void Assemble_hub_next_step_matches_resolver_decision()
    {
        var situation = TestCompetition.Create("Parity", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var expected = StructureHubNextStepResolver.Decide(
            situation.Competition,
            situation.Stages,
            view);

        view.HubNextStep.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Assemble_null_hub_next_step_does_not_imply_readiness_calm()
    {
        var situation = TestCompetition.Create("Not calm", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);

        view.HubNextStep.Should().NotBeNull();
        view.HubNextStep.Code.Should().Be(OverviewAssembler.ActionMaterializeMatches);
        view.Readiness.ReadyForNextSlice.Should().BeTrue();
    }

    [Fact]
    public void Assemble_completed_competition_projects_null_hub_next_step()
    {
        var situation = TestCompetition.Create("Closed", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage()
            .StartPrimaryStage();
        situation.PrimaryStage!.Complete(_clock);
        situation.Competition.Prepare(situation.Clock);
        situation.Competition.Start(situation.Clock);
        situation.Competition.Complete(CompletionMode.Normal, _clock);

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);

        view.HubNextStep.Should().BeNull();
    }
}

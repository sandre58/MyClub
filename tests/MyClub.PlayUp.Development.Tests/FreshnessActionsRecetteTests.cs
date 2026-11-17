// -----------------------------------------------------------------------
// <copyright file="FreshnessActionsRecetteTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Scenarios;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
public sealed class FreshnessActionsRecetteTests(DevelopmentPostgresFixture fixture)
{
    [Fact]
    public async Task Scenario_shows_each_freshness_action_and_the_attention_rowsAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse(FreshnessActionsRecetteNames.ScenarioId)]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stagesRepo = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matchesRepo = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var listed = await competitions.ListAsync();
        var competition = await competitions.GetByIdForUpdateAsync(
            listed.Single(item => item.Name.Value == FreshnessActionsRecetteNames.Competition).Id);
        competition.Should().NotBeNull();
        competition!.Entries.Count(entry => entry.Status == EntryStatus.Active).Should().BeGreaterThanOrEqualTo(4);

        var stages = new List<Stage>();
        var matches = new List<Match>();
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stagesRepo.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();
            stages.Add(stage!);
            stage!.CompositionEntries.Count.Should().BeGreaterThanOrEqualTo(4);
            stage.Status.Should().NotBe(StageStatus.Completed);
            matches.AddRange(await matchesRepo.ListByStageForUpdateAsync(stageId));
        }

        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>();
        foreach (var group in matches.GroupBy(match => match.StageId))
        {
            summaries[group.Key] = group
                .Select(match => new MatchSummaryRow(
                    match.Id,
                    match.StageId,
                    match.Status,
                    match.HomeEntryId,
                    match.AwayEntryId,
                    match.Result,
                    match.HasObservedLive))
                .ToArray();
        }

        var byOccupantStage = ResolutionFreshnessDetector.Evaluate(competition, stages, summaries);
        var occupants = byOccupantStage.SelectMany(pair => pair.Value).ToList();
        var seuleId = StageId(stages, "Seule");

        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Suite"
            && occupant.StalenessCause == StructureResolutionStalenessCause.AmontPerime
            && occupant.UpstreamStageId == StageId(stages, "Tableau"));
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Jouer"
            && occupant.IndeterminacyCause == StructureResolutionIndeterminacyCause.MatchNonJoue
            && occupant.ActionMatchLabel == "Nord – Est");
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Egal"
            && occupant.IndeterminacyCause == StructureResolutionIndeterminacyCause.Egalite
            && occupant.ActionMatchId != null);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Fantome"
            && occupant.IndeterminacyCause == StructureResolutionIndeterminacyCause.SourceAbsente);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Dernier"
            && occupant.IndeterminacyCause == StructureResolutionIndeterminacyCause.SelectionVide
            && occupant.Releasable);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Partage"
            && occupant.IndeterminacyCause == StructureResolutionIndeterminacyCause.EgaliteClassement
            && occupant.Releasable);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Seuil"
            && occupant.IndeterminacyCause == StructureResolutionIndeterminacyCause.ConditionNonRemplie
            && occupant.Releasable);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Libre"
            && occupant.IndeterminacyCause == StructureResolutionIndeterminacyCause.SelectionVide
            && occupant.Releasable);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "NV"
            && occupant.State == StructureResolutionFreshness.NonVerifiee);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Rejouable"
            && occupant.State == StructureResolutionFreshness.Perime
            && occupant.StalenessCause == StructureResolutionStalenessCause.ResultatAmont
            && !occupant.CascadeReplaceable
            && occupant.ProvenanceKind == ResolutionProvenanceKind.Qualification);
        byOccupantStage[seuleId].Should().NotBeEmpty()
            .And.OnlyContain(occupant => occupant.State == StructureResolutionFreshness.NonVerifiee);

        var byStage = matches
            .GroupBy(match => match.StageId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Match>)group.ToList());
        var attention = NeedsAttentionAssembler.Assemble(competition, stages, byStage);
        attention.Items.Should().Contain(item =>
            item.Source == NeedsAttentionAssembler.SourceResolutionPerime
            && item.TargetType == "Slot"
            && item.TargetId!.StartsWith(
                $"{StageId(stages, "Tableau")}:",
                StringComparison.Ordinal)
            && item.Params != null
            && item.Params[NeedsAttentionAssembler.QualificationCoalescedParam] == bool.TrueString);
        attention.Items.Should().NotContain(item =>
            item.Source == NeedsAttentionAssembler.SourceQualificationConflict
            && item.TargetId != null
            && item.TargetId.StartsWith(
                $"{StageId(stages, "Tableau")}:",
                StringComparison.Ordinal));
        attention.Items.Should().Contain(item =>
            item.Source == NeedsAttentionAssembler.SourceResolutionPerime
            && item.TargetId == StageId(stages, "Aval").ToString());
        attention.Items.Should().Contain(item =>
            item.Source == NeedsAttentionAssembler.SourceResolutionIndetermine
            && item.TargetId == StageId(stages, "A jouer").ToString());
        attention.Items.Should().Contain(item =>
            item.Source == NeedsAttentionAssembler.SourceResolutionIndetermine
            && item.TargetId == StageId(stages, "A départager").ToString());
        attention.Items.Should().Contain(item =>
            item.Source == NeedsAttentionAssembler.SourceResolutionIndetermine
            && item.TargetId == StageId(stages, "Orphelin").ToString());
        attention.Items.Should().Contain(item =>
            item.Source == NeedsAttentionAssembler.SourceResolutionIndetermine
            && item.TargetId == StageId(stages, "Classement").ToString());
        attention.Items.Should().Contain(item =>
            item.Source == NeedsAttentionAssembler.SourceResolutionIndetermine
            && item.TargetId == StageId(stages, "Ex aequo").ToString());
        attention.Items.Should().Contain(item =>
            item.Source == NeedsAttentionAssembler.SourceResolutionIndetermine
            && item.TargetId == StageId(stages, "Condition").ToString());
        attention.Items.Should().Contain(item =>
            item.Source == NeedsAttentionAssembler.SourceResolutionIndetermine
            && item.TargetId == StageId(stages, "Généré").ToString());
        attention.Items.Where(item => item.TargetId == seuleId.ToString())
            .Should().NotContain(item =>
                item.Source == NeedsAttentionAssembler.SourceResolutionPerime
                || item.Source == NeedsAttentionAssembler.SourceResolutionIndetermine
                || item.Source == "NonVerifiee");
    }

    private static Guid StageId(IEnumerable<Stage> stages, string name) =>
        stages.Single(stage => stage.Name.Value == name).Id.Value;
}

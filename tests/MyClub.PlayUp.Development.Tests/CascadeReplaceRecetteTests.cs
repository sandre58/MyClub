// -----------------------------------------------------------------------
// <copyright file="CascadeReplaceRecetteTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages.CascadeReplace;
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
public sealed class CascadeReplaceRecetteTests(DevelopmentPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 5, 18, 0, 0, TimeSpan.Zero));

    private static CascadeReplaceTarget Target(Stage stage, string slot) =>
        new(
            StructureResolutionOccupantTarget.Slot,
            stage.FindSlot(slot)!.EntryId!.Value.Value,
            slot,
            null);

    private static async Task<Competition> ReloadAsync(
        ICompetitionRepository competitions,
        Competition listed)
    {
        var competition = await competitions.GetByIdForUpdateAsync(listed.Id);
        competition.Should().NotBeNull();
        return competition!;
    }

    private static Competition Find(IReadOnlyList<Competition> competitions, string name) =>
        competitions.Single(competition => competition.Name.Value == name);

    private static async Task<(List<Stage> Stages, List<Match> Matches)> LoadAsync(
        Competition competition,
        IStageRepository stages,
        IMatchRepository matches)
    {
        var stageList = new List<Stage>(competition.StageIds.Count);
        var matchList = new List<Match>();
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();
            stageList.Add(stage!);
            matchList.AddRange(await matches.ListByStageForUpdateAsync(stageId));
        }

        return (stageList, matchList);
    }

    [Fact]
    public async Task Scenario_covers_the_four_cascade_situationsAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse(CascadeReplaceRecetteNames.ScenarioId)]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var loaded = await competitions.ListAsync();
        loaded.Select(competition => competition.Name.Value).Should().BeEquivalentTo(
        [
            CascadeReplaceRecetteNames.Simple,
            CascadeReplaceRecetteNames.Played,
            CascadeReplaceRecetteNames.Transitive,
            CascadeReplaceRecetteNames.Completed,
            CascadeReplaceRecetteNames.Direct,
        ]);

        await AssertSimpleAsync(await ReloadAsync(competitions, Find(loaded, CascadeReplaceRecetteNames.Simple)), stages, matches);
        await AssertPlayedAsync(await ReloadAsync(competitions, Find(loaded, CascadeReplaceRecetteNames.Played)), stages, matches);
        await AssertTransitiveAsync(await ReloadAsync(competitions, Find(loaded, CascadeReplaceRecetteNames.Transitive)), stages, matches);
        await AssertRefusedAsync(
            await ReloadAsync(competitions, Find(loaded, CascadeReplaceRecetteNames.Completed)),
            stages,
            matches,
            ApplicationErrorCodes.CascadeReplaceCompleted,
            "Champ");
        await AssertRefusedAsync(
            await ReloadAsync(competitions, Find(loaded, CascadeReplaceRecetteNames.Direct)),
            stages,
            matches,
            ApplicationErrorCodes.CascadeReplaceDirectAssignment,
            "Direct");
    }

    private async Task AssertSimpleAsync(
        Competition competition,
        IStageRepository stages,
        IMatchRepository matches)
    {
        var (stageList, matchList) = await LoadAsync(competition, stages, matches);
        var cup = stageList.Single(stage => stage.Name.Value == "Finale");
        var final = matchList.Single(match => match.StageId.Equals(cup.Id));
        final.Status.Should().Be(MatchStatus.Scheduled);
        var before = cup.FindSlot("F-A")!.EntryId!.Value;

        var result = CascadeReplace.Execute(
            competition,
            cup.Id,
            Target(cup, "F-A"),
            stageList,
            matchList,
            _clock);

        result.PlayedMatches.Should().BeEmpty();
        final.Status.Should().Be(MatchStatus.Scheduled);
        final.HomeEntryId.Should().NotBe(before);
        cup.FindSlot("F-A")!.EntryId.Should().NotBe(before);
    }

    private async Task AssertPlayedAsync(
        Competition competition,
        IStageRepository stages,
        IMatchRepository matches)
    {
        var (stageList, matchList) = await LoadAsync(competition, stages, matches);
        var cup = stageList.Single(stage => stage.Name.Value == "Finale");
        var final = matchList.Single(match => match.StageId.Equals(cup.Id));
        final.Status.Should().Be(MatchStatus.Finished);
        final.RecordedGoals.Should().NotBeEmpty();

        var result = CascadeReplace.Execute(
            competition,
            cup.Id,
            Target(cup, "F-A"),
            stageList,
            matchList,
            _clock);

        result.PlayedMatches.Should().ContainSingle().Which.MatchId.Should().Be(final.Id.Value);
        final.Status.Should().Be(MatchStatus.Scheduled);
        final.Result.Should().BeNull();
        final.RecordedGoals.Should().BeEmpty();
        final.DeclaredParticipations.Should().BeEmpty();
    }

    private async Task AssertTransitiveAsync(
        Competition competition,
        IStageRepository stages,
        IMatchRepository matches)
    {
        var (stageList, matchList) = await LoadAsync(competition, stages, matches);
        var middle = stageList.Single(stage => stage.Name.Value == "Milieu");
        var final = stageList.Single(stage => stage.Name.Value == "Finale");
        var group = middle.Groups.Should().ContainSingle().Subject;
        var before = group.EntryIds.Should().ContainSingle().Subject;

        CascadeReplace.Execute(
            competition,
            middle.Id,
            new CascadeReplaceTarget(
                StructureResolutionOccupantTarget.Group,
                before.Value,
                null,
                group.Id.Value),
            stageList,
            matchList,
            _clock);

        var after = group.EntryIds.Should().ContainSingle().Subject;
        after.Should().NotBe(before);
        final.FindSlot("Champ")!.EntryId.Should().Be(after);
        middle.CompositionEntries.Should().NotContain(entry => entry.EntryId.Equals(before));
    }

    private async Task AssertRefusedAsync(
        Competition competition,
        IStageRepository stages,
        IMatchRepository matches,
        string code,
        string slotKey)
    {
        var (stageList, matchList) = await LoadAsync(competition, stages, matches);
        var assessments = ResolutionFreshnessDetector.Inspect(
            competition,
            stageList,
            matchList
                .GroupBy(match => match.StageId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<MatchSummaryRow>)group.Select(match => new MatchSummaryRow(
                        match.Id,
                        match.StageId,
                        match.Status,
                        match.HomeEntryId,
                        match.AwayEntryId,
                        match.Result,
                        match.HasObservedLive)).ToArray()));
        var occupant = assessments.Single(row =>
            row.Target == StructureResolutionOccupantTarget.Slot
            && row.SlotKey == slotKey);

        var act = () => CascadeReplace.Execute(
            competition,
            new StageId(occupant.StageId),
            new CascadeReplaceTarget(occupant.Target, occupant.EntryId, occupant.SlotKey, occupant.GroupId),
            stageList,
            matchList,
            _clock);

        act.Should().Throw<ApplicationFailureException>().Which.Code.Should().Be(code);
    }
}

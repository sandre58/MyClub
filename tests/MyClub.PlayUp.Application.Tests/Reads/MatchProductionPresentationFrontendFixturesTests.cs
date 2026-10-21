// -----------------------------------------------------------------------
// <copyright file="MatchProductionPresentationFrontendFixturesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Keeps committed web fixtures aligned with <see cref="StructureMatchProductionPresentationAssembler"/>.
/// Set UPDATE_MATCH_PRODUCTION_FIXTURES=1 to refresh JSON under web/__fixtures__/matchProduction.
/// </summary>
public sealed class MatchProductionPresentationFrontendFixturesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Running_groups_partial_fixture_matches_assembler_output()
    {
        var presentation = BuildRunningGroupsPartialPresentation();
        AssertMatchesFixture("running-groups-partial.json", presentation);
    }

    [Fact]
    public void Cup_exhausted_fixture_matches_assembler_output()
    {
        var presentation = BuildCupExhaustedPresentation();
        AssertMatchesFixture("cup-exhausted.json", presentation);
    }

    private static void AssertMatchesFixture(
        string fileName,
        StructureMatchProductionPresentationDto presentation)
    {
        MaybeWriteFixture(fileName, presentation);
        var path = FixturePath(fileName);
        File.Exists(path).Should().BeTrue($"fixture missing: {path}");
        var expected = JsonSerializer.Deserialize<StructureMatchProductionPresentationDto>(
            File.ReadAllText(path),
            JsonOptions);
        presentation.Should().BeEquivalentTo(expected);
    }

    private static void MaybeWriteFixture(
        string fileName,
        StructureMatchProductionPresentationDto presentation)
    {
        if (Environment.GetEnvironmentVariable("UPDATE_MATCH_PRODUCTION_FIXTURES") != "1")
        {
            return;
        }

        var path = FixturePath(fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(presentation, JsonOptions));
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
                "matchProduction",
                fileName));

    private static MatchSummaryRow Summary(Match match) =>
        new(match.Id, match.StageId, match.Status, match.HomeEntryId, match.AwayEntryId, match.Result);

    private StructureMatchProductionPresentationDto BuildRunningGroupsPartialPresentation()
    {
        var competition = CreateCompetition.Execute("Running", _clock);
        var e1 = AddEntry.Execute(competition, "A", _clock);
        var e2 = AddEntry.Execute(competition, "B", _clock);
        var e3 = AddEntry.Execute(competition, "C", _clock);
        var e4 = AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        var stage = configured.Stage;
        stage.AssignEntryToGroup(stage.Groups[0].Id, e1.Id);
        stage.AssignEntryToGroup(stage.Groups[0].Id, e2.Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e3.Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e4.Id);
        var (home, away) = MaterializeMatches
            .BuildRoundRobinRounds([.. stage.Groups[0].EntryIds], stage.MatchGenerationFormat)[0][0];
        var fixture = stage.AddFixture(stage.Matchdays[0].Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var hub = StructureViewAssembler.Assemble(
            competition,
            [stage],
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
            {
                [stage.Id] = [Summary(match)]
            }).Stages[0];

        return hub.MatchProductionPresentation
               ?? throw new InvalidOperationException("Expected match production presentation.");
    }

    private StructureMatchProductionPresentationDto BuildCupExhaustedPresentation()
    {
        var situation = TestCompetition.Create("Cup", _clock)
            .WithTeams("Alpha", "Bravo")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
        stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
        stage.Prepare(_clock);
        MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            [],
            [],
            _clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages[0];
        return hub.MatchProductionPresentation
               ?? throw new InvalidOperationException("Expected match production presentation.");
    }
}

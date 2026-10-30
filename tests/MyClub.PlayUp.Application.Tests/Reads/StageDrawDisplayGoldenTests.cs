// -----------------------------------------------------------------------
// <copyright file="StageDrawDisplayGoldenTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Server projection consumed by the web T4 parity test.
/// Set UPDATE_STAGE_DRAW_DISPLAY_FIXTURE=1 to refresh the committed JSON.
/// Guids are omitted; the file is the canonical cell and metric shape.
/// </summary>
public sealed class StageDrawDisplayGoldenTests
{
    private const string FixtureFileName = "stage-draw-display-t4.json";

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string FixturePath() =>
        Path.GetFullPath(Path.Combine(
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
            "draw",
            "fixtures",
            FixtureFileName));

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Partial_cup_matches_the_web_parity_golden()
    {
        var projection = JsonSerializer.SerializeToNode(BuildPartialCupProjection(), JsonOptions);
        var path = FixturePath();
        if (Environment.GetEnvironmentVariable("UPDATE_STAGE_DRAW_DISPLAY_FIXTURE") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, projection!.ToJsonString(JsonOptions) + Environment.NewLine);
        }

        File.Exists(path).Should().BeTrue($"fixture missing: {path}");
        var committed = JsonNode.Parse(File.ReadAllText(path));
        JsonNode.DeepEquals(projection, committed).Should().BeTrue(
            "server projection:{0}{1}{0}committed golden:{0}{2}",
            Environment.NewLine,
            projection!.ToJsonString(JsonOptions),
            committed!.ToJsonString(JsonOptions));
    }

    private object BuildPartialCupProjection()
    {
        var (competition, stage, entries) = Cup(2);
        stage.AssignEntryToSlot(stage.Slots[0].SlotKey, entries[0]);
        var draw = ResolvePartial(stage);
        var overview = StageOverviewAssembler.Assemble(stage, competition);
        var active = overview.Draws.Single(item => item.Id == draw.Id.Value);

        return new
        {
            active.InputsIntent,
            active.DisplayMetrics,
            SlotDisplayCells = active.SlotDisplayCells!.Select(cell => new
            {
                cell.SlotKey,
                cell.CellState,
                cell.DisplayName,
                cell.Origin,
            }).ToArray(),
            BracketPairs = overview.BracketPairs.Select(pair => new
            {
                pair.PairKey,
                pair.SlotAKey,
                pair.SlotBKey,
            }).ToArray(),
        };
    }

    private Draw ResolvePartial(Stage stage)
    {
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        var inputs = DrawInputsFactory.CreatePartial(stage, [stage], DrawResolutionKind.Slot);
        ConfigureDrawInputs.Execute(stage, draw.Id, inputs, DrawInputsIntent.Partial, [stage]);
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            stage.Slots.Select(slot => slot.SlotKey).ToArray(),
            competitionStages: [stage],
            randomSource: new SeededRandomSource(3));
        return draw;
    }

    private (Competition Competition, Stage Stage, EntryId[] Entries) Cup(int slots)
    {
        var competition = CreateCompetition.Execute($"Cup {slots}", _clock);
        var entries = Enumerable.Range(0, slots)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(slots), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(entries, _clock);
        return (competition, stage, entries);
    }
}

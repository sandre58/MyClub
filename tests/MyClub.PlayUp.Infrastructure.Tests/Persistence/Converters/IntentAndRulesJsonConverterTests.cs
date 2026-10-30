// -----------------------------------------------------------------------
// <copyright file="IntentAndRulesJsonConverterTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence.Converters;

public sealed class IntentAndRulesJsonConverterTests
{
    [Fact]
    public void ProgressionIntent_round_trips_via_stage_regulation_json_options()
    {
        var roundId = RoundId.New();
        var destinationStageId = StageId.New();
        var intent = new ProgressionIntent(
            IntentId.New(),
            order: 1,
            roundId,
            ProgressionOutcome.Winner,
            destinationStageId,
            destinationSlotKeys: ["SF1"]);

        var json = JsonSerializer.Serialize(intent, StageRegulationJson.Options);
        var restored = JsonSerializer.Deserialize<ProgressionIntent>(json, StageRegulationJson.Options);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be(intent.Id);
        restored.Order.Should().Be(intent.Order);
        restored.RoundId.Should().Be(intent.RoundId);
        restored.Outcome.Should().Be(intent.Outcome);
        restored.DestinationStageId.Should().Be(intent.DestinationStageId);
        restored.DestinationSlotKeys.Should().Equal("SF1");
    }

    [Fact]
    public void ProgressionIntent_reads_legacy_nested_destination_with_slot_key()
    {
        var intentId = IntentId.New();
        var roundId = RoundId.New();
        var destinationStageId = StageId.New();
        var json =
            $$"""
            {
              "Id":"{{intentId.Value}}",
              "Order":1,
              "RoundId":"{{roundId.Value}}",
              "Outcome":0,
              "Destination":{
                "StageId":"{{destinationStageId.Value}}",
                "SlotKey":"W1"
              }
            }
            """;

        var restored = JsonSerializer.Deserialize<ProgressionIntent>(json, StageRegulationJson.Options);

        restored!.DestinationStageId.Should().Be(destinationStageId);
        restored.DestinationSlotKeys.Should().Equal("W1");
    }

    [Fact]
    public void QualificationIntent_round_trips_and_reads_legacy_singular_slot_key()
    {
        var groupId = GroupId.New();
        var destinationStageId = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.SingleGroup,
            positionFrom: 1,
            positionTo: 1,
            destinationStageId,
            groupId: groupId,
            condition: QualificationCondition.PointsAtLeast(4),
            destinationSlotKeys: ["A", "B"]);

        var json = JsonSerializer.Serialize(intent, StageRegulationJson.Options);
        var restored = JsonSerializer.Deserialize<QualificationIntent>(json, StageRegulationJson.Options);
        restored.Should().NotBeNull();
        restored!.Id.Should().Be(intent.Id);
        restored.SourceKind.Should().Be(intent.SourceKind);
        restored.DestinationSlotKeys.Should().Equal("A", "B");

        var legacyJson = json.Replace(
            "\"DestinationSlotKeys\"",
            "\"DestinationSlotKey\"",
            StringComparison.Ordinal)
            .Replace("[\"A\",\"B\"]", "\"A\"", StringComparison.Ordinal);
        var fromLegacy = JsonSerializer.Deserialize<QualificationIntent>(legacyJson, StageRegulationJson.Options);
        fromLegacy!.DestinationSlotKeys.Should().Equal("A");
    }

    [Fact]
    public void ProgressionRules_round_trips_intents_and_reads_paths_only_json()
    {
        var destinationStageId = StageId.New();
        var roundId = RoundId.New();
        var intent = new ProgressionIntent(
            IntentId.New(),
            1,
            roundId,
            ProgressionOutcome.Loser,
            destinationStageId,
            ["L1"]);
        var path = new ProgressionPath(
            "P1",
            ProgressionOutcome.Loser,
            ProgressionDestination.ForPopulation(destinationStageId));

        var withIntents = ProgressionRules.FromPersisted([intent], [path]);
        var json = JsonSerializer.Serialize(withIntents, StageRegulationJson.Options);
        JsonSerializer.Deserialize<ProgressionRules>(json, StageRegulationJson.Options).Should().Be(withIntents);

        var pathsOnly = new ProgressionRules([path]);
        var pathsJson = JsonSerializer.Serialize(pathsOnly, StageRegulationJson.Options);
        var fromPaths = JsonSerializer.Deserialize<ProgressionRules>(pathsJson, StageRegulationJson.Options);
        fromPaths!.Paths.Should().ContainSingle();
        fromPaths.Intents.Should().BeEmpty();
    }

    [Fact]
    public void QualificationRules_round_trips_intents_and_reads_paths_only_json()
    {
        var groupId = GroupId.New();
        var destinationStageId = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.SingleGroup,
            1,
            1,
            destinationStageId,
            groupId);
        var path = new QualificationPath(
            1,
            QualificationSource.FromGroup(groupId),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForPopulation(destinationStageId));

        var withIntents = QualificationRules.FromPersisted([intent], [path]);
        var json = JsonSerializer.Serialize(withIntents, StageRegulationJson.Options);
        JsonSerializer.Deserialize<QualificationRules>(json, StageRegulationJson.Options).Should().Be(withIntents);

        var pathsOnly = new QualificationRules([path]);
        var pathsJson = JsonSerializer.Serialize(pathsOnly, StageRegulationJson.Options);
        var fromPaths = JsonSerializer.Deserialize<QualificationRules>(pathsJson, StageRegulationJson.Options);
        fromPaths!.Paths.Should().ContainSingle().Which.Should().Be(path);
    }
}

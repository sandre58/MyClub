// -----------------------------------------------------------------------
// <copyright file="TieFormatEntryRoundConfrontationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class TieFormatEntryRoundConfrontationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 27, 20, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Entry_round_effective_format_is_stable_before_fixture_materialization()
    {
        var situation = TestCompetition.Create("Cup-two-rounds", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));

        var stage = situation.RequirePrimaryStage();
        var qf = stage.Rounds[0];
        var sf = stage.AddRound("SF", _clock);
        stage.ReplaceRoundTieFormat(qf.Id, new TieFormat(TieFormat.TwoLegs, aggregateScoring: true), _clock);
        stage.ReplaceRoundTieFormat(
            sf.Id,
            new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
            _clock);
        stage.SeedEntryRoundBracketPairs();

        var pairKey = stage.BracketPairs[0].PairKey;
        var before = stage.ResolveEffectiveTieFormatForKey(pairKey);

        before.NumberOfLegs.Should().Be(TieFormat.TwoLegs);
        stage.FindRoundForConfrontationKey(pairKey).Should().Be(qf);

        stage.FindRoundForConfrontationKey(pairKey).Should().Be(qf);
        stage.ResolveEffectiveTieFormatForKey(pairKey).Should().Be(before);
    }

    [Fact]
    public void Entry_round_effective_format_and_execution_diagnostics_stable_after_materialization()
    {
        var situation = TestCompetition.Create("Cup-two-rounds-mat", _clock)
            .WithTeams(4)
            .WithStructure(StructureIntent.Cup(4));

        var stage = situation.RequirePrimaryStage();
        var entryRound = stage.Rounds[0];
        _ = stage.AddRound("SF", _clock);
        var decidableTwoLegs = new TieFormat(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            penaltyShootoutRule: new PenaltyShootoutRule());
        stage.ReplaceRoundTieFormat(entryRound.Id, decidableTwoLegs, _clock);
        stage.ReplaceRoundTieFormat(
            stage.Rounds[1].Id,
            new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
            _clock);

        var entries = situation.Competition.Entries.ToArray();
        for (var index = 0; index < stage.Slots.Count; index++)
        {
            stage.ApplyResolvedEntry(stage.Slots[index].SlotKey, entries[index].Id, _clock);
        }

        stage.Prepare(_clock);

        var pairKey = stage.BracketPairs[0].PairKey;
        var effectiveBefore = stage.ResolveEffectiveTieFormatForKey(pairKey);
        var diagnosticsBefore = CompetitionFlowAnalyzer
            .AnalyzeExecution([stage])
            .Where(diagnostic => diagnostic.SlotKey == pairKey)
            .ToArray();

        MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            [],
            [],
            _clock,
            planEntireRound: true);

        var effectiveAfter = stage.ResolveEffectiveTieFormatForKey(pairKey);
        var diagnosticsAfter = CompetitionFlowAnalyzer
            .AnalyzeExecution([stage])
            .Where(diagnostic => diagnostic.SlotKey == pairKey)
            .ToArray();

        effectiveAfter.Should().BeEquivalentTo(effectiveBefore);
        diagnosticsAfter.Should().BeEquivalentTo(diagnosticsBefore);
    }
}

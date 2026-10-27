// -----------------------------------------------------------------------
// <copyright file="ReplaceStageProgressionRulesGOpsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// <see cref="ReplaceStageProgressionRules"/> on Ready upstream.
/// </summary>
public sealed class ReplaceStageProgressionRulesGOpsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 15, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ReplaceProgressionRules_refuses_when_ready_upstream_has_attached_matches_D2()
    {
        var competition = CreateCompetition.Execute("G-OPS-D2-prog", _clock);
        var quarter = Stage.Create(
            competition.Id,
            new StageName("Quarts"),
            SampleRegulations.Standard(),
            _clock);
        var round = quarter.AddRound("QF", tieFormat: null, _clock);
        quarter.AddFixture(round.Id, _clock);
        quarter.AddSlot("KO-A");
        quarter.AddSlot("KO-B");
        quarter.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        var a = EntryId.New();
        var b = EntryId.New();
        quarter.ReplaceAffectationAuthoring([a, b], _clock);
        quarter.AssignEntryToSlot("KO-A", a);
        quarter.AssignEntryToSlot("KO-B", b);
        PrepareStage.Execute(quarter, [quarter], _clock);

        var materialize = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            quarter,
            ["P1"],
            [],
            _clock);
        var attached = materialize.CreatedMatches.ToList();
        attached.Should().NotBeEmpty();

        var act = () => ReplaceStageProgressionRules.Execute(
            quarter,
            (IReadOnlyList<ProgressionPathSpec>?)null,
            _clock,
            [quarter],
            attached);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void ReplaceProgressionRules_allows_narrowing_when_downstream_draft()
    {
        var (quarter, final, stages) = CreateCupQuarterAndFinal();
        quarter.Prepare(_clock);
        quarter.Status.Should().Be(StageStatus.Ready);
        final.Status.Should().Be(StageStatus.Draft);

        var act = () => ReplaceStageProgressionRules.Execute(
            quarter,
            [
                new ProgressionPathSpec("P1", ProgressionOutcome.Winner, final.Id)
            ],
            _clock,
            stages,
            [],
            new Dictionary<StageId, IReadOnlyList<Match>> { [final.Id] = [] });

        act.Should().NotThrow();
        quarter.Regulation.ProgressionRules!.Paths.Should().ContainSingle();
    }

    [Fact]
    public void ReplaceProgressionRules_refuses_when_ready_consumer_has_more_entries_than_inbound_volume()
    {
        var (quarter, final, stages) = CreateCupQuarterAndFinal();
        var e1 = EntryId.New();
        var e2 = EntryId.New();
        final.AddResolvedPopulationEntry(e1, _clock);
        final.AddResolvedPopulationEntry(e2, _clock);

        quarter.Prepare(_clock);
        final.Prepare(_clock);
        final.CompositionEntries.Should().HaveCount(2);

        var rulesBefore = quarter.Regulation.ProgressionRules;

        var act = () => ReplaceStageProgressionRules.Execute(
            quarter,
            [
                new ProgressionPathSpec("P1", ProgressionOutcome.Winner, final.Id)
            ],
            _clock,
            stages,
            []);

        var ex = act.Should().Throw<ApplicationFailureException>().Which;
        ex.Code.Should().Be(ApplicationErrorCodes.ReadyConsumerWouldBreak);
        ex.Reasons.Should().Contain(final.Id.Value.ToString());
        quarter.Regulation.ProgressionRules.Should().BeEquivalentTo(rulesBefore);
    }

    [Fact]
    public void ReplaceProgressionRules_refuses_when_intents_cleared_on_ready_consumer()
    {
        var (quarter, final, stages) = CreateCupQuarterAndFinal();
        final.AddResolvedPopulationEntry(EntryId.New(), _clock);
        final.AddResolvedPopulationEntry(EntryId.New(), _clock);
        quarter.Prepare(_clock);
        final.Prepare(_clock);

        var act = () => ReplaceStageProgressionRules.Execute(
            quarter,
            (IReadOnlyList<ProgressionIntentSpec>?)null,
            _clock,
            stages,
            []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ReadyConsumerWouldBreak);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Reasons.Should().Contain(final.Id.Value.ToString());
    }

    private (Stage Quarter, Stage Final, List<Stage> Stages) CreateCupQuarterAndFinal()
    {
        var competition = CreateCompetition.Execute("G-OPS-prog", _clock);
        var quarter = Stage.Create(
            competition.Id,
            new StageName("Quarts"),
            SampleRegulations.Standard(),
            _clock);
        var round = quarter.AddRound("QF", tieFormat: null, _clock);
        quarter.AddSlot("KO-A");
        quarter.AddSlot("KO-B");
        quarter.AddSlot("KO-C");
        quarter.AddSlot("KO-D");
        quarter.ReplaceBracketPairs(
        [
            new BracketPair("P1", "KO-A", "KO-B"),
            new BracketPair("P2", "KO-C", "KO-D")
        ]);
        quarter.AddFixture(round.Id, _clock, "KO-A", "KO-B", "P1");
        var a = EntryId.New();
        var b = EntryId.New();
        var c = EntryId.New();
        var d = EntryId.New();
        quarter.ReplaceAffectationAuthoring([a, b, c, d], _clock);
        quarter.AssignEntryToSlot("KO-A", a);
        quarter.AssignEntryToSlot("KO-B", b);
        quarter.AssignEntryToSlot("KO-C", c);
        quarter.AssignEntryToSlot("KO-D", d);

        var final = Stage.Create(
            competition.Id,
            new StageName("Finale"),
            SampleRegulations.Standard(),
            _clock);
        final.AddMatchday(1, _clock);

        quarter.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath("P1", ProgressionOutcome.Winner, ProgressionDestination.ForPopulation(final.Id)),
                new ProgressionPath("P2", ProgressionOutcome.Winner, ProgressionDestination.ForPopulation(final.Id))
            ]),
            _clock);

        return (quarter, final, [quarter, final]);
    }
}

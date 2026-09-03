// -----------------------------------------------------------------------
// <copyright file="PipelineCompositionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Cross-mechanism composition (7.0.8.2): Host orchestrates Qualification/Progression/Draw without Domain fusion.
/// Host must supply Draw entry pools from progressed/qualified populations (Domain does not enforce ⊆).
/// </summary>
public sealed class PipelineCompositionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 11, 16, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Progression_then_Draw_Apply_uses_progressed_entries_as_host_pool()
    {
        var competitionId = CompetitionId.New();
        var qf = CreateKnockout(competitionId, "QF", ["QF1-A", "QF1-B", "QF2-A", "QF2-B"]);
        var bridge = CreateKnockout(competitionId, "Winners", ["W1", "W2"]);
        var sf = CreateKnockout(competitionId, "SF", ["SF1-A", "SF1-B"]);

        var a = EntryId.New();
        var b = EntryId.New();
        var c = EntryId.New();
        var d = EntryId.New();
        var (fx1, m1) = AttachFinishedMatch(qf, a, b, homeGoals: 2, awayGoals: 0);
        var (fx2, m2) = AttachFinishedMatch(qf, c, d, homeGoals: 0, awayGoals: 1);

        qf.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(fx1, ProgressionOutcome.Winner, new ProgressionDestination(bridge.Id, "W1")),
                new ProgressionPath(fx2, ProgressionOutcome.Winner, new ProgressionDestination(bridge.Id, "W2"))
            ]),
            _clock);

        ApplyProgressionOutcome.Execute(qf, fx1, [m1], [qf, bridge], _clock);
        ApplyProgressionOutcome.Execute(qf, fx2, [m2], [qf, bridge], _clock);

        var pool = new[]
        {
            bridge.FindSlot("W1")!.EntryId!.Value,
            bridge.FindSlot("W2")!.EntryId!.Value
        };
        pool.Should().BeEquivalentTo([a, d]);

        var draw = sf.CreateDraw(DrawResolutionKind.Slot, _clock);
        sf.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(pool));
        sf.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(pool[0], "SF1-A"),
                new SlotDrawPlacement(pool[1], "SF1-B")
            ]),
            _clock);
        sf.PublishDraw(draw.Id, _clock);

        ApplyDraw.Execute(sf, draw.Id, _clock);

        sf.FindSlot("SF1-A")!.EntryId.Should().Be(pool[0]);
        sf.FindSlot("SF1-B")!.EntryId.Should().Be(pool[1]);
        bridge.FindSlot("W1")!.EntryId.Should().Be(pool[0]);
        draw.Inputs!.Entries.Should().BeEquivalentTo(pool);
        sf.DirectAssignments.Should().BeEmpty();
    }

    [Fact]
    public void Pairing_Draw_Match_Outcome_then_Progression_keeps_boundaries()
    {
        var competitionId = CompetitionId.New();
        var stage = CreateKnockout(competitionId, "KO", ["SF1-A", "Consolante"]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var home = EntryId.New();
        var away = EntryId.New();

        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing([home, away]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedPairings([new PairingDrawResult(home, away)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);

        var created = ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(fixture.Id),
            []);
        created.CreatedMatches.Should().ContainSingle();
        var match = created.CreatedMatches[0];
        Finish(match, homeGoals: 3, awayGoals: 1);

        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A")),
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Loser,
                    new ProgressionDestination(stage.Id, "Consolante"))
            ]),
            _clock);

        ApplyProgressionOutcome.Execute(stage, fixture.Id, [match], [stage], _clock);

        stage.FindSlot("SF1-A")!.EntryId.Should().Be(home);
        stage.FindSlot("Consolante")!.EntryId.Should().Be(away);
        draw.Status.Should().Be(DrawStatus.Published);
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
        fixture.MatchIds.Should().ContainSingle().Which.Should().Be(match.Id);
    }

    [Fact]
    public void Qualification_then_Progression_without_Draw_composes()
    {
        var competitionId = CompetitionId.New();
        var league = Stage.Create(competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        var qf = CreateKnockout(competitionId, "QF", ["QF1-A", "QF1-B"]);
        var sf = CreateKnockout(competitionId, "SF", ["SF1-A"]);
        var entries = new[] { EntryId.New(), EntryId.New() };
        var rr = Match.Create(competitionId, league.Id, entries[0], entries[1], _clock);
        Finish(rr, 3, 0);
        var standing = CalculateStanding.Execute(
            entries,
            [rr],
            league.Regulation.StandingRules);

        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    new QualificationDestination(qf.Id, "QF1-A")),
                new QualificationPath(
                    2,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 2),
                    new QualificationDestination(qf.Id, "QF1-B"))
            ]),
            _clock);
        ApplyQualification.Execute(league, standing, [league, qf], _clock);

        var home = qf.FindSlot("QF1-A")!.EntryId!.Value;
        var away = qf.FindSlot("QF1-B")!.EntryId!.Value;
        var (fixtureId, match) = AttachFinishedMatch(qf, home, away, 2, 1);
        qf.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(sf.Id, "SF1-A"))
            ]),
            _clock);
        ApplyProgressionOutcome.Execute(qf, fixtureId, [match], [qf, sf], _clock);

        sf.FindSlot("SF1-A")!.EntryId.Should().Be(home);
        league.Draws.Should().BeEmpty();
        qf.Draws.Should().BeEmpty();
        sf.Draws.Should().BeEmpty();
        sf.DirectAssignments.Should().BeEmpty();
    }

    [Fact]
    public void Group_Draw_Apply_then_Qualification_keeps_boundaries()
    {
        var competitionId = CompetitionId.New();
        var groups = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var groupA = groups.AddGroup("A", _clock);
        var groupB = groups.AddGroup("B", _clock);
        groups.AddMatchday(1, _clock);
        var a = EntryId.New();
        var b = EntryId.New();
        var c = EntryId.New();
        var d = EntryId.New();
        var draw = groups.CreateDraw(DrawResolutionKind.Group, _clock);
        groups.ConfigureDrawInputs(draw.Id, DrawInputs.ForGroup([a, b, c, d]));
        groups.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedGroups(
            [
                new GroupDrawPlacement(a, groupA.Id),
                new GroupDrawPlacement(b, groupA.Id),
                new GroupDrawPlacement(c, groupB.Id),
                new GroupDrawPlacement(d, groupB.Id)
            ]),
            _clock);
        groups.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(groups, draw.Id, _clock);

        groupA.EntryIds.Should().BeEquivalentTo([a, b]);
        groupB.EntryIds.Should().BeEquivalentTo([c, d]);

        var terminal = CreateKnockout(competitionId, "Terminal", ["Champ"]);
        var m1 = Match.Create(competitionId, groups.Id, a, b, _clock);
        Finish(m1, 2, 0);
        var m2 = Match.Create(competitionId, groups.Id, c, d, _clock);
        Finish(m2, 1, 0);
        var standing = CalculateStanding.Execute(
            [a, b, c, d],
            [m1, m2],
            groups.Regulation.StandingRules);
        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    new QualificationDestination(terminal.Id, "Champ"))
            ]),
            _clock);
        ApplyQualification.Execute(groups, standing, [groups, terminal], _clock);

        terminal.FindSlot("Champ")!.EntryId.Should().NotBeNull();
        draw.Status.Should().Be(DrawStatus.Published);
        draw.Kind.Should().Be(DrawResolutionKind.Group);
        groups.DirectAssignments.Should().BeEmpty();
        terminal.Draws.Should().BeEmpty();
    }

    [Fact]
    public void Two_phase_Draw_then_Progression_then_Draw_composes()
    {
        var competitionId = CompetitionId.New();
        var qf = CreateKnockout(competitionId, "QF", ["X"]);
        var bridge = CreateKnockout(competitionId, "Bridge", ["W1", "W2"]);
        var sf = CreateKnockout(competitionId, "SF", ["SF1-A", "SF1-B"]);

        var a = EntryId.New();
        var b = EntryId.New();
        var c = EntryId.New();
        var d = EntryId.New();
        var fxQf1 = qf.AddFixture(qf.Rounds[0].Id, _clock);
        var fxQf2 = qf.AddFixture(qf.Rounds[0].Id, _clock);

        var drawQf1 = PublishPairing(qf, [a, b], new PairingDrawResult(a, b));
        var drawQf2 = PublishPairing(qf, [c, d], new PairingDrawResult(c, d));
        var created1 = ApplyDraw.Execute(qf, drawQf1.Id, _clock, new PairingApplicationContext(fxQf1.Id), []);
        var created2 = ApplyDraw.Execute(qf, drawQf2.Id, _clock, new PairingApplicationContext(fxQf2.Id), []);
        var m1 = created1.CreatedMatches[0];
        var m2 = created2.CreatedMatches[0];
        Finish(m1, 2, 0);
        Finish(m2, 0, 1);

        qf.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(fxQf1.Id, ProgressionOutcome.Winner, new ProgressionDestination(bridge.Id, "W1")),
                new ProgressionPath(fxQf2.Id, ProgressionOutcome.Winner, new ProgressionDestination(bridge.Id, "W2"))
            ]),
            _clock);
        ApplyProgressionOutcome.Execute(qf, fxQf1.Id, [m1], [qf, bridge], _clock);
        ApplyProgressionOutcome.Execute(qf, fxQf2.Id, [m2], [qf, bridge], _clock);

        EntryId[] pool =
        [
            bridge.FindSlot("W1")!.EntryId!.Value,
            bridge.FindSlot("W2")!.EntryId!.Value
        ];
        pool.Should().BeEquivalentTo([a, d]);

        var drawSf = sf.CreateDraw(DrawResolutionKind.Slot, _clock);
        sf.ConfigureDrawInputs(drawSf.Id, DrawInputs.ForSlot(pool));
        sf.RecordDrawResolution(
            drawSf.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(pool[0], "SF1-A"),
                new SlotDrawPlacement(pool[1], "SF1-B")
            ]),
            _clock);
        sf.PublishDraw(drawSf.Id, _clock);
        ApplyDraw.Execute(sf, drawSf.Id, _clock);

        sf.FindSlot("SF1-A")!.EntryId.Should().Be(pool[0]);
        sf.FindSlot("SF1-B")!.EntryId.Should().Be(pool[1]);
        drawQf1.Id.Should().NotBe(drawSf.Id);
        drawQf1.Status.Should().Be(DrawStatus.Published);
        drawSf.Status.Should().Be(DrawStatus.Published);
        drawQf1.Resolution.PairingResults.Should().ContainSingle();
        drawSf.Resolution.SlotResults.Should().HaveCount(2);
    }

    private Draw PublishPairing(Stage stage, EntryId[] pool, PairingDrawResult pairing)
    {
        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing(pool));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedPairings([pairing]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        return draw;
    }

    private Stage CreateKnockout(CompetitionId competitionId, string name, string[] slotKeys)
    {
        var stage = Stage.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key, _clock);
        }

        return stage;
    }

    private (FixtureId FixtureId, Match Match) AttachFinishedMatch(
        Stage stage,
        EntryId home,
        EntryId away,
        int homeGoals,
        int awayGoals)
    {
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        Finish(match, homeGoals, awayGoals);
        return (fixture.Id, match);
    }

    private void Finish(Match match, int homeGoals, int awayGoals)
    {
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
    }
}

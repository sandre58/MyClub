// -----------------------------------------------------------------------
// <copyright file="BackfillResolutionProvenanceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class BackfillResolutionProvenanceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Direct_assignment_slot_is_backfilled_as_Directe()
    {
        var (competition, terminal, _) = CreateTerminalWithDirectSlot("A");
        var report = RunBackfill(competition, [terminal]);

        report.SkippedAlreadyPresent.Should().BeGreaterThan(0);
        report.BackfilledExact.Should().Be(0);
        terminal.FindSlot("A")!.ResolutionProvenance!.Kind.Should().Be(ResolutionProvenanceKind.Directe);
    }

    [Fact]
    public void Qualification_slot_with_matching_replay_is_backfilled_exact()
    {
        var (competition, league, terminal, matches, winner) = CreateQualificationSlotScenario();
        terminal.ApplyResolvedEntry(
            "Champ",
            winner,
            _clock,
            ResolutionProvenance.NonVerifiee());

        var report = RunBackfill(competition, [league, terminal], matches);

        report.BackfilledExact.Should().BeGreaterThan(0);
        terminal.FindSlot("Champ")!.ResolutionProvenance!.Kind.Should().Be(ResolutionProvenanceKind.Qualification);
    }

    [Fact]
    public void Qualification_slot_with_foreign_occupant_is_NonVerifiee()
    {
        var (competition, league, terminal, matches, _) = CreateQualificationSlotScenario();
        var foreign = EntryId.New();
        terminal.ApplyResolvedEntry("Champ", foreign, _clock, ResolutionProvenance.NonVerifiee());

        var report = RunBackfill(competition, [league, terminal], matches);

        report.NonVerifiee.Should().BeGreaterThan(0);
        terminal.FindSlot("Champ")!.ResolutionProvenance!.Kind.Should().Be(ResolutionProvenanceKind.NonVerifiee);
    }

    [Fact]
    public void Multiple_feeds_on_slot_yields_NonVerifiee()
    {
        var competition = Competition.Create(new CompetitionName("Mixed"), SampleRegulations.Standard(), _clock);
        var source = Stage.Create(competition.Id, new StageName("Source"), SampleRegulations.Standard(), _clock);
        source.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("A");
        source.AddSlot("B");
        source.SeedEntryRoundBracketPairs();
        var pairKey = source.BracketPairs[0].PairKey;
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("SF1-A");
        var entry = competition.AddEntry(TeamId.New(), "A", _clock).Id;
        terminal.AddResolvedPopulationEntry(entry, _clock, ResolutionProvenance.NonVerifiee());
        terminal.ApplyResolvedEntry("SF1-A", entry, _clock, ResolutionProvenance.NonVerifiee());

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(terminal.Id, "SF1-A"))
            ]),
            _clock);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    pairKey,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForSlot(terminal.Id, "SF1-A"))
            ]),
            _clock);

        var report = RunBackfill(competition, [source, terminal]);

        report.NonVerifiee.Should().BeGreaterThan(0);
        terminal.FindSlot("SF1-A")!.ResolutionProvenance!.Kind.Should().Be(ResolutionProvenanceKind.NonVerifiee);
    }

    [Fact]
    public void Second_run_on_same_state_preserves_provenance_without_rewriting()
    {
        var (competition, league, terminal, matches, winner) = CreateQualificationSlotScenario();
        terminal.ApplyResolvedEntry("Champ", winner, _clock, ResolutionProvenance.NonVerifiee());

        var first = RunBackfill(competition, [league, terminal], matches);
        var provenanceAfterFirst = terminal.FindSlot("Champ")!.ResolutionProvenance;
        provenanceAfterFirst!.Kind.Should().Be(ResolutionProvenanceKind.Qualification);
        var occupantBefore = terminal.FindSlot("Champ")!.EntryId;

        var second = RunBackfill(competition, [league, terminal], matches);

        second.BackfilledExact.Should().Be(0);
        second.SkippedAlreadyPresent.Should().BeGreaterThan(0);
        terminal.FindSlot("Champ")!.EntryId.Should().Be(occupantBefore);
        terminal.FindSlot("Champ")!.ResolutionProvenance.Should().Be(provenanceAfterFirst);
        first.Categories.Qualification.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Backfill_after_real_apply_qualification_preserves_exact_provenance()
    {
        var (competition, league, terminal, matches, _) = CreateQualificationSlotScenario();
        var standing = CalculateStanding.Execute(
            [.. competition.Entries.Select(entry => entry.Id)],
            matches,
            league.Regulation.StandingRules.OrThrow());

        ApplyQualification.Execute(league, standing, [league, terminal], _clock);

        var before = terminal.FindSlot("Champ")!.ResolutionProvenance;
        before.Should().NotBeNull();
        before!.Kind.Should().Be(ResolutionProvenanceKind.Qualification);

        var report = RunBackfill(competition, [league, terminal], matches);

        report.BackfilledExact.Should().Be(0);
        report.SkippedAlreadyPresent.Should().BeGreaterThan(0);
        terminal.FindSlot("Champ")!.ResolutionProvenance.Should().Be(before);
    }

    private static BackfillResolutionProvenanceReport RunBackfill(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match>? matches = null)
    {
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? byStage = null;
        if (matches is { Count: > 0 })
        {
            byStage = matches
                .GroupBy(match => match.StageId)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<Match>)[.. group]);
        }

        return BackfillResolutionProvenance.Execute(competition, stages, byStage);
    }

    private (Competition Competition, Stage Terminal, EntryId EntryId) CreateTerminalWithDirectSlot(string slotKey)
    {
        var competition = Competition.Create(new CompetitionName("Direct"), SampleRegulations.Standard(), _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot(slotKey);
        var entryId = competition.AddEntry(TeamId.New(), "Team", _clock).Id;
        terminal.AddResolvedPopulationEntry(entryId, _clock, ResolutionProvenance.NonVerifiee());
        terminal.AssignEntryToSlot(slotKey, entryId);
        return (competition, terminal, entryId);
    }

    private (
        Competition Competition,
        Stage League,
        Stage Terminal,
        List<Match> Matches,
        EntryId Winner) CreateQualificationSlotScenario()
    {
        var competition = Competition.Create(new CompetitionName("Qual"), SampleRegulations.Standard(), _clock);
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        _ = league.AddMatchday(1, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("Champ");

        var entries = Enumerable.Range(0, 3).Select(_ => competition.AddEntry(TeamId.New(), "T", _clock).Id).ToArray();
        var matches = new List<Match>();
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                var match = Match.Create(competition.Id, league.Id, entries[i], entries[j], _clock);
                match.Start(_clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
                matches.Add(match);
            }
        }

        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules.OrThrow());
        var winner = standing.EntryAt(1)!.Value;

        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(terminal.Id, "Champ"))
            ]),
            _clock);

        return (competition, league, terminal, matches, winner);
    }
}

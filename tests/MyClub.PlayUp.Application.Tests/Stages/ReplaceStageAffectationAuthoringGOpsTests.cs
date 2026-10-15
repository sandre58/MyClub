// -----------------------------------------------------------------------
// <copyright file="ReplaceStageAffectationAuthoringGOpsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// G-OPS1 (D2 only) — <see cref="ReplaceStageAffectationAuthoring"/> on Ready upstream.
/// </summary>
public sealed class ReplaceStageAffectationAuthoringGOpsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 15, 15, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ReplaceAffectationAuthoring_allows_edit_when_ready_without_attached_matches()
    {
        var competition = CreateCompetition.Execute("G-OPS-aff-allow", _clock);
        var stage = AddCompetitionStage.Execute(
            competition,
            StructureIntent.Championship(stageName: "Phase"),
            _clock);
        var e1 = competition.AddEntry(TeamId.New(), "A", _clock).Id;
        var e2 = competition.AddEntry(TeamId.New(), "B", _clock).Id;
        ReplaceStageAffectationAuthoring.Execute(stage, competition, [e1], _clock);
        PrepareStage.Execute(stage, [stage], _clock);
        stage.Status.Should().Be(StageStatus.Ready);

        ReplaceStageAffectationAuthoring.Execute(stage, competition, [e1, e2], _clock, []);

        stage.AffectationAuthoring.Select(e => e.EntryId).Should().Equal(e1, e2);
    }

    [Fact]
    public void ReplaceAffectationAuthoring_refuses_when_ready_upstream_has_attached_matches_D2()
    {
        var competition = CreateCompetition.Execute("G-OPS-aff-D2", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("QF", tieFormat: null, _clock);
        stage.AddFixture(round.Id, _clock);
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        var a = competition.AddEntry(TeamId.New(), "A", _clock).Id;
        var b = competition.AddEntry(TeamId.New(), "B", _clock).Id;
        stage.ReplaceAffectationAuthoring([a, b], _clock);
        stage.AssignEntryToSlot("KO-A", a);
        stage.AssignEntryToSlot("KO-B", b);
        PrepareStage.Execute(stage, [stage], _clock);

        var materialize = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);
        var attached = materialize.CreatedMatches.ToList();

        var act = () => ReplaceStageAffectationAuthoring.Execute(
            stage,
            competition,
            [a],
            _clock,
            [stage],
            attached);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void ReplaceAffectationAuthoring_allows_shrink_when_promised_feed_still_covers_ready_consumer()
    {
        var (competition, upstream, downstream, stages, entries) = CreateQualFeedWithAffectationTopology();
        upstream.Prepare(_clock);
        downstream.Prepare(_clock);

        var act = () => ReplaceStageAffectationAuthoring.Execute(
            upstream,
            competition,
            [entries[0], entries[1]],
            _clock,
            stages,
            []);

        act.Should().NotThrow();
        upstream.AffectationAuthoring.Should().HaveCount(2);
    }

    [Fact]
    public void ReplaceAffectationAuthoring_refuses_shrink_when_source_population_breaks_ready_consumer_D1()
    {
        var (competition, upstream, downstream, stages, entries) = CreateQualFeedWithAffectationTopology();
        upstream.Prepare(_clock);
        downstream.Prepare(_clock);
        downstream.CompositionEntries.Should().HaveCount(2);

        var before = upstream.AffectationAuthoring.Select(e => e.EntryId).ToArray();

        var act = () => ReplaceStageAffectationAuthoring.Execute(
            upstream,
            competition,
            [entries[0]],
            _clock,
            stages,
            []);

        var ex = act.Should().Throw<ApplicationFailureException>().Which;
        ex.Code.Should().Be(ApplicationErrorCodes.ReadyConsumerWouldBreak);
        ex.Reasons.Should().Contain(downstream.Id.Value.ToString());
        upstream.AffectationAuthoring.Select(e => e.EntryId).Should().Equal(before);
    }

    private (Competition Competition, Stage Upstream, Stage Downstream, List<Stage> Stages, EntryId[] Entries)
        CreateQualFeedWithAffectationTopology()
    {
        var competition = CreateCompetition.Execute("G-OPS-aff-D1", _clock);
        var upstream = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            SampleRegulations.Standard(),
            _clock);
        upstream.AddMatchday(1, _clock);

        var downstream = Stage.Create(
            competition.Id,
            new StageName("Phase suivante"),
            SampleRegulations.Standard(),
            _clock);
        downstream.AddMatchday(1, _clock);

        var entries = Enumerable.Range(0, 4)
            .Select(_ => competition.AddEntry(TeamId.New(), $"T{_}", _clock).Id)
            .ToArray();

        upstream.ReplaceAffectationAuthoring(entries, _clock);
        upstream.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Top, 2),
                    QualificationDestination.ForPopulation(downstream.Id))
            ]),
            _clock);

        foreach (var entryId in entries.Take(2))
        {
            downstream.AddResolvedPopulationEntry(entryId, _clock);
        }

        return (competition, upstream, downstream, [upstream, downstream], entries);
    }
}

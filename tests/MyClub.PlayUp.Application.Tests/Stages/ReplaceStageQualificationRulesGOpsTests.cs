// -----------------------------------------------------------------------
// <copyright file="ReplaceStageQualificationRulesGOpsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// G-OPS1 / G-OPS2 slice 1 — <see cref="ReplaceStageQualificationRules"/> on Ready upstream (R-D1a/b).
/// </summary>
public sealed class ReplaceStageQualificationRulesGOpsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ReplaceQualificationRules_refuses_when_ready_upstream_has_attached_matches_D2()
    {
        var competition = CreateCompetition.Execute("G-OPS-D2-qual", _clock);
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
        var a = EntryId.New();
        var b = EntryId.New();
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
        materialize.CreatedMatches.Should().NotBeEmpty();
        var attached = materialize.CreatedMatches.ToList();

        var act = () => ReplaceStageQualificationRules.Execute(
            stage,
            (IReadOnlyList<QualificationPathSpec>?)null,
            _clock,
            [stage],
            attached);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void ReplaceQualificationRules_R_D1a_allows_top2_to_top1_when_downstream_draft()
    {
        var (upstream, downstream, stages) = CreateQualFeedTopology(applyQualification: false);
        PrepareStage.Execute(upstream, stages, _clock);
        upstream.Status.Should().Be(StageStatus.Ready);
        downstream.Status.Should().Be(StageStatus.Draft);

        var act = () => ReplaceWithTopOne(upstream, downstream, stages);

        act.Should().NotThrow();
        upstream.Regulation.QualificationRules!.Paths.Should().ContainSingle()
            .Which.Selection.Value.Should().Be(1);
    }

    [Fact]
    public void ReplaceQualificationRules_R_D1b_refuses_top2_to_top1_when_ready_consumer_w3()
    {
        var (upstream, downstream, stages) = CreateQualFeedTopology(applyQualification: true);
        PrepareStage.Execute(upstream, stages, _clock);
        PrepareStage.Execute(downstream, stages, _clock);
        upstream.Status.Should().Be(StageStatus.Ready);
        downstream.Status.Should().Be(StageStatus.Ready);
        downstream.CompositionEntries.Should().HaveCount(2);

        var rulesBefore = upstream.Regulation.QualificationRules;
        var downstreamStatusBefore = downstream.Status;

        var act = () => ReplaceWithTopOne(upstream, downstream, stages);

        var ex = act.Should().Throw<ApplicationFailureException>().Which;
        ex.Code.Should().Be(ApplicationErrorCodes.ReadyConsumerWouldBreak);
        ex.Reasons.Should().Contain(downstream.Id.Value.ToString());

        upstream.Regulation.QualificationRules.Should().BeEquivalentTo(rulesBefore);
        downstream.Status.Should().Be(downstreamStatusBefore);
        downstream.CompositionEntries.Should().HaveCount(2);
    }

    [Fact]
    public void ReplaceQualificationRules_R_D1b_refuses_clear_qual_when_ready_consumer_has_placed_entries()
    {
        var (upstream, downstream, stages) = CreateQualFeedTopology(applyQualification: true);
        PrepareStage.Execute(upstream, stages, _clock);
        PrepareStage.Execute(downstream, stages, _clock);

        var rulesBefore = upstream.Regulation.QualificationRules;

        var act = () => ReplaceStageQualificationRules.Execute(
            upstream,
            (IReadOnlyList<QualificationPathSpec>?)null,
            _clock,
            stages,
            []);

        var ex = act.Should().Throw<ApplicationFailureException>().Which;
        ex.Code.Should().Be(ApplicationErrorCodes.ReadyConsumerWouldBreak);
        ex.Reasons.Should().Contain(downstream.Id.Value.ToString());
        upstream.Regulation.QualificationRules.Should().BeEquivalentTo(rulesBefore);
    }

    [Fact]
    public void ReplaceQualificationRules_R_D1b_refuses_when_intents_cleared_on_ready_consumer()
    {
        var (upstream, downstream, stages) = CreateQualFeedTopology(applyQualification: true);
        upstream.Prepare(_clock);
        downstream.Prepare(_clock);

        var act = () => ReplaceStageQualificationRules.Execute(
            upstream,
            (IReadOnlyList<QualificationIntentSpec>?)null,
            _clock,
            stages,
            []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ReadyConsumerWouldBreak);
    }

    private void ReplaceWithTopOne(Stage upstream, Stage downstream, IReadOnlyList<Stage> stages) =>
        ReplaceStageQualificationRules.Execute(
            upstream,
            [
                new QualificationPathSpec(
                    1,
                    SelectionMode.Top,
                    1,
                    downstream.Id.Value,
                    RankingScope.Overall)
            ],
            _clock,
            stages,
            []);

    private (Stage Upstream, Stage Downstream, List<Stage> Stages) CreateQualFeedTopology(bool applyQualification)
    {
        var competitionId = CompetitionId.New();
        var upstream = Stage.Create(
            competitionId,
            new StageName("Poules"),
            SampleRegulations.Standard(),
            _clock);
        upstream.AddMatchday(1, _clock);

        var downstream = Stage.Create(
            competitionId,
            new StageName("Phase suivante"),
            SampleRegulations.Standard(),
            _clock);
        downstream.AddMatchday(1, _clock);

        var stages = new List<Stage> { upstream, downstream };
        var entries = Enumerable.Range(0, 4).Select(_ => EntryId.New()).ToArray();

        foreach (var entryId in entries)
        {
            upstream.AddResolvedPopulationEntry(entryId, _clock);
        }

        if (applyQualification)
        {
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

            downstream.CompositionEntries.Should().HaveCount(2);
        }
        else
        {
            ReplaceStageQualificationRules.Execute(
                upstream,
                [
                    new QualificationPathSpec(
                        1,
                        SelectionMode.Top,
                        2,
                        downstream.Id.Value,
                        RankingScope.Overall)
                ],
                _clock);
        }

        return (upstream, downstream, stages);
    }
}

// -----------------------------------------------------------------------
// <copyright file="ReplaceStageQualificationRulesAfterApplyTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Qualification rule replacement after an Apply: status lock lifts, consumer and draw locks stay.
/// </summary>
public sealed class ReplaceStageQualificationRulesAfterApplyTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 19, 0, 0, TimeSpan.Zero));

    private static QualificationPath OverallPath(int position, StageId destinationId) =>
        new(
            position,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            QualificationDestination.ForSlot(destinationId, "Champ"));

    private static Dictionary<StageId, IReadOnlyList<MatchSummaryRow>> Summaries(
        Stage stage,
        IReadOnlyList<Match> matches) =>
        new()
        {
            [stage.Id] = StageStartReadinessGuard.ToSummaryRows(matches),
        };

    [Fact]
    public void Running_stage_accepts_a_volume_stable_rule_change_after_apply_and_marks_occupants_stale()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed();
        ApplyPosition(league, terminal, entries, matches, 1);
        var stored = terminal.FindSlot("Champ")!.EntryId!.Value;
        PrepareStage.Execute(league, [league, terminal], _clock);
        league.Start(_clock);
        league.Status.Should().Be(StageStatus.Running);

        var act = () => ReplacePosition(league, terminal, [league, terminal], matches, 2);

        act.Should().NotThrow();
        league.Regulation.QualificationRules!.Paths.Should().ContainSingle()
            .Which.Selection.Value.Should().Be(2);
        terminal.FindSlot("Champ")!.EntryId.Should().Be(stored);
        ResolutionFreshnessDetector.Evaluate(
                competition,
                [league, terminal],
                Summaries(league, matches))
            .SelectMany(pair => pair.Value)
            .Should().Contain(occupant =>
                occupant.Target == StructureResolutionOccupantTarget.Slot
                && occupant.EntryId == stored.Value
                && occupant.State == StructureResolutionFreshness.Perime
                && occupant.StalenessCause == StructureResolutionStalenessCause.RegleQualification);
    }

    [Fact]
    public void Running_stage_without_apply_still_refuses_qualification_rule_changes()
    {
        var (_, league, terminal, _, matches) = LeagueFeed();
        ReplacePosition(league, terminal, [league, terminal], [], 1);
        PrepareStage.Execute(league, [league, terminal], _clock);
        league.Start(_clock);

        var act = () => ReplacePosition(league, terminal, [league, terminal], matches, 2);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void Completed_stage_still_refuses_qualification_rule_changes_after_apply()
    {
        var (_, league, terminal, entries, matches) = LeagueFeed();
        ApplyPosition(league, terminal, entries, matches, 1);
        PrepareStage.Execute(league, [league, terminal], _clock);
        league.Start(_clock);
        CompleteStage.Execute(league, matches, _clock);

        var act = () => ReplacePosition(league, terminal, [league, terminal], matches, 2);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void Ready_consumer_volume_break_is_kept_after_apply_on_a_running_source()
    {
        var (_, league, terminal, entries, matches) = LeagueFeed();
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules!);
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(terminal.Id)),
                new QualificationPath(
                    2,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 2),
                    QualificationDestination.ForPopulation(terminal.Id)),
            ]),
            _clock);
        ApplyQualification.Execute(league, standing, [league, terminal], _clock);
        terminal.CompositionEntries.Should().HaveCount(2);
        PrepareStage.Execute(league, [league, terminal], _clock);
        PrepareStage.Execute(terminal, [league, terminal], _clock);
        league.Start(_clock);
        terminal.Status.Should().Be(StageStatus.Ready);

        var act = () => ReplaceStageQualificationRules.Execute(
            league,
            [
                new QualificationPathSpec(
                    1,
                    SelectionMode.Position,
                    1,
                    terminal.Id.Value,
                    RankingScope.Overall),
            ],
            _clock,
            [league, terminal],
            matches);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ReadyConsumerWouldBreak);
        league.Regulation.QualificationRules!.Paths.Should().HaveCount(2);
    }

    [Fact]
    public void Applied_draw_still_blocks_qualification_rule_changes_after_apply_on_a_running_source()
    {
        var competition = CreateCompetition.Execute("Applied draw lock", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var downstream = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock).Stage;
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var matches = RoundRobin(stage, ids);
        var standing = CalculateStanding.Execute(ids, matches, stage.Regulation.StandingRules!);
        stage.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(downstream.Id)),
            ]),
            _clock);
        ApplyQualification.Execute(stage, standing, [stage, downstream], _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(group => group.Id)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        PrepareStage.Execute(stage, [stage, downstream], _clock);
        stage.Start(_clock);

        var act = () => ReplaceStageQualificationRules.Execute(
            stage,
            [
                new QualificationPathSpec(
                    1,
                    SelectionMode.Position,
                    2,
                    downstream.Id.Value,
                    RankingScope.Overall),
            ],
            _clock,
            [stage, downstream],
            matches);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawFeedRulesChangeBlocked);
    }

    private (Competition Competition, Stage League, Stage Terminal, EntryId[] Entries, List<Match> Matches) LeagueFeed()
    {
        var competition = CreateCompetition.Execute($"Gate {Guid.NewGuid():N}", _clock);
        var entries = new EntryId[3];
        for (var index = 0; index < entries.Length; index++)
        {
            entries[index] = AddEntry.Execute(competition, $"T{index}", _clock).Id;
        }

        Array.Sort(entries, (left, right) => left.Value.CompareTo(right.Value));
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddMatchday(1, _clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, _clock);
        return (competition, league, terminal, entries, RoundRobin(league, entries));
    }

    private void ApplyPosition(
        Stage league,
        Stage terminal,
        EntryId[] entries,
        IReadOnlyList<Match> matches,
        int position)
    {
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules!);
        league.ReplaceQualificationRules(
            new QualificationRules([OverallPath(position, terminal.Id)]),
            _clock);
        ApplyQualification.Execute(league, standing, [league, terminal], _clock);
    }

    private void ReplacePosition(
        Stage league,
        Stage terminal,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches,
        int position) =>
        ReplaceStageQualificationRules.Execute(
            league,
            [
                new QualificationPathSpec(
                    1,
                    SelectionMode.Position,
                    position,
                    terminal.Id.Value,
                    RankingScope.Overall,
                    DestinationSlotKey: "Champ"),
            ],
            _clock,
            stages,
            matches);

    private List<Match> RoundRobin(Stage stage, EntryId[] entries)
    {
        var matches = new List<Match>();
        for (var home = 0; home < entries.Length; home++)
        {
            for (var away = home + 1; away < entries.Length; away++)
            {
                var match = Match.Create(stage.CompetitionId, stage.Id, entries[home], entries[away], _clock);
                match.Start(_clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(entries.Length - home, 0)), _clock);
                matches.Add(match);
            }
        }

        return matches;
    }
}

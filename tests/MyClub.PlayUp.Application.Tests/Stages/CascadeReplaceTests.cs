// -----------------------------------------------------------------------
// <copyright file="CascadeReplaceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Stages.CascadeReplace;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using Xunit;
using ApplicationFailureException = MyClub.PlayUp.Application.ApplicationFailureException;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class CascadeReplaceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 5, 16, 0, 0, TimeSpan.Zero));

    private static QualificationPath OverallPath(int position, StageId destinationId, string? slot)
    {
        var destination = slot is null
            ? QualificationDestination.ForPopulation(destinationId)
            : QualificationDestination.ForSlot(destinationId, slot);
        return new QualificationPath(
            position,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            destination);
    }

    private static CascadeReplaceTarget SlotTarget(Stage stage, string slot) =>
        new(
            StructureResolutionOccupantTarget.Slot,
            stage.FindSlot(slot)!.EntryId!.Value.Value,
            slot,
            null);

    [Fact]
    public void Cup_three_rounds_replaces_the_finalist_and_invalidates_only_the_final()
    {
        var (competition, stages, quarter, semi, final, entries, quarterMatch, semiMatch, finalMatch) =
            CupThreeRounds();
        var oldFinalist = final.FindSlot("F-A")!.EntryId!.Value;
        semiMatch.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 2)), _clock);
        var newFinalist = entries[2];

        var result = CascadeReplace.Execute(
            competition,
            final.Id,
            SlotTarget(final, "F-A"),
            stages,
            [quarterMatch, semiMatch, finalMatch],
            _clock);

        final.FindSlot("F-A")!.EntryId.Should().Be(newFinalist);
        final.FindSlot("F-A")!.EntryId.Should().NotBe(oldFinalist);
        final.FindSlot("F-B")!.EntryId.Should().Be(entries[3]);
        semi.FindSlot("SF-A")!.EntryId.Should().Be(entries[0]);
        semi.FindSlot("SF-B")!.EntryId.Should().Be(entries[2]);
        quarter.FindSlot("QF-A")!.EntryId.Should().Be(entries[0]);
        finalMatch.Status.Should().Be(MatchStatus.Scheduled);
        finalMatch.Result.Should().BeNull();
        finalMatch.HomeEntryId.Should().Be(newFinalist);
        finalMatch.RecordedGoals.Should().BeEmpty();
        finalMatch.DeclaredParticipations.Should().BeEmpty();
        semiMatch.Status.Should().Be(MatchStatus.Finished);
        semiMatch.Result!.Score.Should().Be(new Score(0, 2));
        quarterMatch.Status.Should().Be(MatchStatus.Finished);
        result.PlayedMatches.Should().ContainSingle().Which.MatchId.Should().Be(finalMatch.Id.Value);
        StandingMatchAssembler.Assemble([finalMatch]).Should().BeEmpty();
        final.CompositionEntries.Should().NotContain(entry => entry.EntryId.Equals(oldFinalist));
    }

    [Fact]
    public void Unplayed_downstream_match_is_overwritten_without_a_played_warning()
    {
        var (competition, league, cup, entries, matches) = LeagueFeed(2);
        var fixture = CupRound(cup, "F-A", "F-B");
        ApplyOverall(league, cup, entries, matches, 1, "F-A");
        var applied = cup.FindSlot("F-A")!.EntryId!.Value;
        var match = Match.Create(competition.Id, cup.Id, applied, awayEntryId: null, _clock);
        cup.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        SetScore(matches, entries[0], entries[1], 0, 2);

        var result = CascadeReplace.Execute(
            competition,
            cup.Id,
            SlotTarget(cup, "F-A"),
            [league, cup],
            [.. matches, match],
            _clock);

        result.PlayedMatches.Should().BeEmpty();
        match.Status.Should().Be(MatchStatus.Scheduled);
        match.HomeEntryId.Should().Be(entries[1]);
        cup.FindSlot("F-A")!.EntryId.Should().Be(entries[1]);
    }

    [Fact]
    public void Transitive_group_chain_replaces_the_middle_member_and_the_final_slot()
    {
        var competition = CreateCompetition.Execute("Chain", _clock);
        var entries = AddTeams(competition, 2);
        var groups = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var group = groups.AddGroup("A", _clock);
        groups.AssignEntryToGroup(group.Id, entries[0]);
        groups.AssignEntryToGroup(group.Id, entries[1]);
        competition.AddStage(groups.Id, _clock);
        var groupMatch = Match.Create(competition.Id, groups.Id, entries[0], entries[1], _clock);
        groupMatch.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        var standing = CalculateStanding.Execute(
            [entries[0], entries[1]],
            [groupMatch],
            groups.Regulation.StandingRules!);

        var middle = Stage.Create(competition.Id, new StageName("Middle"), SampleRegulations.Standard(), _clock);
        var middleGroup = middle.AddGroup("M", _clock);
        competition.AddStage(middle.Id, _clock);
        var final = Stage.Create(competition.Id, new StageName("Final"), SampleRegulations.Standard(), _clock);
        final.AddSlot("Champ");
        competition.AddStage(final.Id, _clock);
        var stages = new List<Stage> { groups, middle, final };

        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(group.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(middle.Id, middleGroup.Id)),
            ]),
            _clock);
        ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [group.Id] = standing },
            stages,
            _clock);
        var qualified = middleGroup.EntryIds.Should().ContainSingle().Subject;
        var middleStanding = CalculateStanding.Execute(
            middleGroup.EntryIds.ToArray(),
            Array.Empty<Match>(),
            middle.Regulation.StandingRules!);
        middle.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(middleGroup.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(final.Id, "Champ")),
            ]),
            _clock);
        ApplyQualification.Execute(
            middle,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [middleGroup.Id] = middleStanding },
            stages,
            _clock);
        groupMatch.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 2)), _clock);

        CascadeReplace.Execute(
            competition,
            middle.Id,
            new CascadeReplaceTarget(
                StructureResolutionOccupantTarget.Group,
                qualified.Value,
                null,
                middleGroup.Id.Value),
            stages,
            [groupMatch],
            _clock);

        middleGroup.EntryIds.Should().ContainSingle().Which.Should().Be(entries[1]);
        middle.CompositionEntries.Should().NotContain(entry => entry.EntryId.Equals(entries[0]));
        final.FindSlot("Champ")!.EntryId.Should().Be(entries[1]);
        final.CompositionEntries.Should().NotContain(entry => entry.EntryId.Equals(entries[0]));
    }

    [Fact]
    public void Completed_phase_blocks_the_cascade()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(2);
        ApplyOverall(league, terminal, entries, matches, 1, "Champ");
        terminal.AddMatchday(1, _clock);
        PrepareStage.Execute(terminal, [league, terminal], _clock);
        terminal.Start(_clock);
        terminal.Complete(_clock);
        SetScore(matches, entries[0], entries[1], 0, 2);

        var act = () => CascadeReplace.Execute(
            competition,
            terminal.Id,
            SlotTarget(terminal, "Champ"),
            [league, terminal],
            matches,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.CascadeReplaceCompleted);
    }

    [Fact]
    public void Direct_assignment_is_refused()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(2);
        terminal.AddSlot("Direct");
        terminal.AddResolvedPopulationEntry(entries[1], _clock);
        terminal.AssignEntryToSlot("Direct", entries[1]);

        var act = () => CascadeReplace.Execute(
            competition,
            terminal.Id,
            new CascadeReplaceTarget(
                StructureResolutionOccupantTarget.Slot,
                entries[1].Value,
                "Direct",
                null),
            [league, terminal],
            matches,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.CascadeReplaceDirectAssignment);
    }

    [Fact]
    public void Mutual_qualification_is_a_cycle()
    {
        var competition = CreateCompetition.Execute("Cycle", _clock);
        var entries = AddTeams(competition, 2);
        var alpha = Stage.Create(competition.Id, new StageName("Alpha"), SampleRegulations.Standard(), _clock);
        var beta = Stage.Create(competition.Id, new StageName("Beta"), SampleRegulations.Standard(), _clock);
        alpha.AddMatchday(1, _clock);
        beta.AddMatchday(1, _clock);
        competition.AddStage(alpha.Id, _clock);
        competition.AddStage(beta.Id, _clock);
        var alphaMatches = RoundRobin(alpha, entries);
        var betaMatches = RoundRobin(beta, entries);
        ApplyOverall(alpha, beta, entries, alphaMatches, 1, null);
        ApplyOverall(beta, alpha, entries, betaMatches, 1, null);
        alpha.ReplaceQualificationRules(
            new QualificationRules([OverallPath(2, beta.Id, null)]),
            _clock);
        var stored = beta.CompositionEntries.Should().ContainSingle().Subject.EntryId;

        var act = () => CascadeReplace.Execute(
            competition,
            beta.Id,
            new CascadeReplaceTarget(
                StructureResolutionOccupantTarget.Population,
                stored.Value,
                null,
                null),
            [alpha, beta],
            [.. alphaMatches, .. betaMatches],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.CascadeReplaceCycle);
    }

    [Fact]
    public void Replacement_changes_the_confirmation_token()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(2);
        ApplyOverall(league, terminal, entries, matches, 1, "Champ");
        SetScore(matches, entries[0], entries[1], 0, 2);
        var stages = new List<Stage> { league, terminal };
        var first = CascadeReplace.Execute(
            competition,
            terminal.Id,
            SlotTarget(terminal, "Champ"),
            stages,
            matches,
            _clock);

        var act = () =>
        {
            var current = CascadeReplaceVersion.Compute(stages, matches);
            current.Should().NotBe(first.VersionToken);
        };

        act.Should().NotThrow();
        terminal.FindSlot("Champ")!.EntryId.Should().Be(entries[1]);
    }

    private (
        Competition Competition,
        List<Stage> Stages,
        Stage Quarter,
        Stage Semi,
        Stage Final,
        EntryId[] Entries,
        Match QuarterMatch,
        Match SemiMatch,
        Match FinalMatch) CupThreeRounds()
    {
        var competition = CreateCompetition.Execute("Cup 3", _clock);
        var entries = AddTeams(competition, 4);
        var quarter = Stage.Create(competition.Id, new StageName("Quarter"), SampleRegulations.Standard(), _clock);
        var semi = Stage.Create(competition.Id, new StageName("Semi"), SampleRegulations.Standard(), _clock);
        var final = Stage.Create(competition.Id, new StageName("Final"), SampleRegulations.Standard(), _clock);
        competition.AddStage(quarter.Id, _clock);
        competition.AddStage(semi.Id, _clock);
        competition.AddStage(final.Id, _clock);
        var stages = new List<Stage> { quarter, semi, final };

        var quarterFixture = CupRound(quarter, "QF-A", "QF-B");
        Direct(quarter, "QF-A", entries[0]);
        Direct(quarter, "QF-B", entries[1]);
        var quarterMatch = Play(competition, quarter, quarterFixture, entries[0], entries[1], 1, 0);
        Progress(quarter, quarterFixture, quarterMatch, semi, "SF-A", stages);

        var semiFixture = CupRound(semi, "SF-A", "SF-B");
        Direct(semi, "SF-B", entries[2]);
        var semiMatch = Play(competition, semi, semiFixture, entries[0], entries[2], 2, 0);
        Progress(semi, semiFixture, semiMatch, final, "F-A", stages);

        var finalFixture = CupRound(final, "F-A", "F-B");
        Direct(final, "F-B", entries[3]);
        var finalMatch = Play(competition, final, finalFixture, entries[0], entries[3], 3, 0);
        var scorer = MemberId.New();
        finalMatch.AddDeclaredParticipation(scorer, Side.Home, CompositionStatus.Starter, _clock, jerseyNumber: 9);
        finalMatch.RecordGoal(scorer, Side.Home, _clock);
        return (competition, stages, quarter, semi, final, entries, quarterMatch, semiMatch, finalMatch);
    }

    private void Progress(
        Stage source,
        Fixture fixture,
        Match match,
        Stage destination,
        string slot,
        IReadOnlyList<Stage> stages)
    {
        if (destination.FindSlot(slot) is null)
        {
            destination.AddSlot(slot);
        }

        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(destination.Id, slot)),
            ]),
            _clock);
        ApplyProgressionOutcome.Execute(source, fixture.Id, [match], stages, _clock);
    }

    private Match Play(
        Competition competition,
        Stage stage,
        Fixture fixture,
        EntryId home,
        EntryId away,
        int homeGoals,
        int awayGoals)
    {
        var match = Match.Create(competition.Id, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
        return match;
    }

    private void Direct(Stage stage, string slot, EntryId entry)
    {
        stage.AddResolvedPopulationEntry(entry, _clock);
        stage.AssignEntryToSlot(slot, entry);
    }

    private Fixture CupRound(Stage stage, string slotA, string slotB)
    {
        stage.AddRound("R", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        if (stage.FindSlot(slotA) is null)
        {
            stage.AddSlot(slotA);
        }

        if (stage.FindSlot(slotB) is null)
        {
            stage.AddSlot(slotB);
        }

        stage.ReplaceBracketPairs([new BracketPair("P1", slotA, slotB)]);
        return stage.AddFixture(stage.Rounds[0].Id, _clock, slotA, slotB, "P1");
    }

    private (Competition Competition, Stage League, Stage Terminal, EntryId[] Entries, List<Match> Matches) LeagueFeed(
        int teams)
    {
        var competition = CreateCompetition.Execute($"League {teams} {Guid.NewGuid():N}", _clock);
        var entries = AddTeams(competition, teams);
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, _clock);
        return (competition, league, terminal, entries, RoundRobin(league, entries));
    }

    private void ApplyOverall(
        Stage source,
        Stage destination,
        EntryId[] entries,
        IReadOnlyList<Match> matches,
        int position,
        string? slot)
    {
        var standing = CalculateStanding.Execute(entries, matches, source.Regulation.StandingRules!);
        source.ReplaceQualificationRules(
            new QualificationRules([OverallPath(position, destination.Id, slot)]),
            _clock);
        ApplyQualification.Execute(source, standing, [source, destination], _clock);
    }

    private EntryId[] AddTeams(Competition competition, int count)
    {
        var ids = new EntryId[count];
        for (var index = 0; index < count; index++)
        {
            ids[index] = AddEntry.Execute(competition, $"T{index}", _clock).Id;
        }

        Array.Sort(ids, (left, right) => left.Value.CompareTo(right.Value));
        return ids;
    }

    private List<Match> RoundRobin(Stage stage, EntryId[] entries)
    {
        var matches = new List<Match>();
        for (var home = 0; home < entries.Length; home++)
        {
            for (var away = home + 1; away < entries.Length; away++)
            {
                var match = Match.Create(stage.CompetitionId, stage.Id, entries[home], entries[away], _clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(entries.Length - home, 0)), _clock);
                matches.Add(match);
            }
        }

        return matches;
    }

    private void SetScore(IReadOnlyList<Match> matches, EntryId home, EntryId away, int homeGoals, int awayGoals)
    {
        var match = matches.Single(candidate =>
            candidate.HomeEntryId.Equals(home) && candidate.AwayEntryId.Equals(away));
        match.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
    }
}

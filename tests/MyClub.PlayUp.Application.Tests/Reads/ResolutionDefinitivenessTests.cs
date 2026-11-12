// -----------------------------------------------------------------------
// <copyright file="ResolutionDefinitivenessTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages.ReleaseResolution;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class ResolutionDefinitivenessTests
{
    private static readonly FakeClock Clock = new(new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero));

    public static IEnumerable<object[]> Matrix()
    {
        var causes = new (StructureResolutionIndeterminacyCause Cause, string Absence)[]
        {
            (StructureResolutionIndeterminacyCause.MatchNonJoue, string.Empty),
            (StructureResolutionIndeterminacyCause.Egalite, string.Empty),
            (StructureResolutionIndeterminacyCause.EgaliteClassement, string.Empty),
            (StructureResolutionIndeterminacyCause.ConditionNonRemplie, string.Empty),
            (StructureResolutionIndeterminacyCause.SelectionVide, string.Empty),
            (StructureResolutionIndeterminacyCause.SourceAbsente, nameof(ResolutionSourceAbsence.StageNotLoaded)),
            (StructureResolutionIndeterminacyCause.SourceAbsente, nameof(ResolutionSourceAbsence.PathGone)),
            (StructureResolutionIndeterminacyCause.SourceAbsente, nameof(ResolutionSourceAbsence.Unreadable)),
            (StructureResolutionIndeterminacyCause.ConfigurationInvalide, string.Empty),
            (StructureResolutionIndeterminacyCause.Autre, string.Empty),
        };
        var sourceStatuses = new[] { "Draft", "Ready", "Running", "Suspended", "Completed", "Deleted" };
        var scopes = new[] { "Group", "Overall", "AcrossGroups" };
        var competitions = new[] { CompetitionStatus.Running, CompetitionStatus.Suspended, CompetitionStatus.Completed };
        foreach (var (cause, absence) in causes)
        {
            foreach (var sourceStatus in sourceStatuses)
            {
                foreach (var scope in scopes)
                {
                    foreach (var competition in competitions)
                    {
                        yield return [cause, absence, sourceStatus, scope, competition];
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public void Definitive_follows_the_cause_table(
        StructureResolutionIndeterminacyCause cause,
        string absenceName,
        string sourceStatus,
        string scope,
        CompetitionStatus competitionStatus)
    {
        var absence = string.IsNullOrEmpty(absenceName)
            ? (ResolutionSourceAbsence?)null
            : Enum.Parse<ResolutionSourceAbsence>(absenceName);
        var (competition, source, destination) = World(competitionStatus, sourceStatus);
        var parsedId = source?.Id.Value ?? Guid.NewGuid();
        var occupant = Occupant(destination, cause, absence, cause == StructureResolutionIndeterminacyCause.SourceAbsente ? parsedId : source?.Id.Value);
        var path = Path(scope, destination.Id);

        var verdict = ResolutionDefinitiveness.Evaluate(
            competition,
            absence == ResolutionSourceAbsence.StageNotLoaded ? null : source,
            path,
            occupant,
            destination,
            []);

        var sourceCompleted = source is { Status: StageStatus.Completed }
            && absence != ResolutionSourceAbsence.StageNotLoaded;
        var idInCompetition = source is not null && competition.StageIds.Any(id => id.Value == source.Id.Value);
        verdict.Definitive.Should().Be(Expected(cause, absence, sourceCompleted, scope == "Overall", competitionStatus, idInCompetition, parsedId, competition));
    }

    [Fact]
    public void Overall_standing_causes_stay_open_while_the_competition_can_withdraw_a_team()
    {
        foreach (var cause in new[]
        {
            StructureResolutionIndeterminacyCause.EgaliteClassement,
            StructureResolutionIndeterminacyCause.ConditionNonRemplie,
        })
        {
            var running = JudgeStanding(cause, "Overall", CompetitionStatus.Running, StageStatus.Completed);
            var suspended = JudgeStanding(cause, "Overall", CompetitionStatus.Suspended, StageStatus.Completed);
            var closed = JudgeStanding(cause, "Overall", CompetitionStatus.Completed, StageStatus.Completed);
            var group = JudgeStanding(cause, "Group", CompetitionStatus.Running, StageStatus.Completed);
            running.Should().BeFalse();
            suspended.Should().BeFalse();
            closed.Should().BeTrue();
            group.Should().BeTrue();
        }
    }

    [Fact]
    public void SelectionVide_on_overall_with_fallback_participants_is_not_definitive()
    {
        var (competition, source, destination) = World(CompetitionStatus.Running, "Completed");
        foreach (var entry in competition.Entries.ToArray())
        {
            competition.WithdrawEntry(entry.Id, Clock);
        }

        var verdict = ResolutionDefinitiveness.Evaluate(
            competition,
            source,
            Path("Overall", destination.Id),
            Occupant(destination, StructureResolutionIndeterminacyCause.SelectionVide, null, source!.Id.Value),
            destination,
            []);

        verdict.Definitive.Should().BeFalse();
        competition.Entries.Should().NotContain(entry => entry.Status == EntryStatus.Active);
    }

    [Fact]
    public void A_readable_source_id_absent_from_the_competition_is_definitive()
    {
        var (competition, _, destination) = World(CompetitionStatus.Running, "Deleted");
        var missing = Guid.NewGuid();
        competition.StageIds.Should().NotContain(id => id.Value == missing);

        var verdict = ResolutionDefinitiveness.Evaluate(
            competition,
            source: null,
            qualificationPath: null,
            Occupant(destination, StructureResolutionIndeterminacyCause.SourceAbsente, ResolutionSourceAbsence.StageNotLoaded, missing),
            destination,
            []);

        verdict.Definitive.Should().BeTrue();
    }

    [Fact]
    public void An_unreadable_source_id_is_never_definitive()
    {
        var (competition, source, destination) = World(CompetitionStatus.Completed, "Completed");
        var verdict = ResolutionDefinitiveness.Evaluate(
            competition,
            source,
            null,
            Occupant(destination, StructureResolutionIndeterminacyCause.SourceAbsente, ResolutionSourceAbsence.Unreadable, null),
            destination,
            []);
        verdict.Definitive.Should().BeFalse();
    }

    [Fact]
    public void A_partial_stage_list_does_not_make_a_place_definitive()
    {
        var (competition, league, terminal, _, _) = QualifiedSlot(position: 9);
        Move(league, StageStatus.Completed);

        var occupants = ResolutionFreshnessDetector.Evaluate(
            competition,
            [terminal],
            Summaries(league));
        var place = occupants.SelectMany(pair => pair.Value).Single(occupant => occupant.SlotKey == "Champ");
        place.IndeterminacyCause.Should().Be(StructureResolutionIndeterminacyCause.SourceAbsente);
        place.Definitive.Should().BeFalse();
        competition.StageIds.Should().Contain(id => id.Equals(league.Id));

        var inspected = ResolutionFreshnessDetector.Inspect(competition, [terminal], Summaries(league))
            .Single(occupant => occupant.SlotKey == "Champ");
        inspected.SourceAbsence.Should().Be(ResolutionSourceAbsence.StageNotLoaded);
        inspected.ParsedSourceStageId.Should().Be(league.Id.Value);
    }

    [Fact]
    public void A_removed_phase_is_definitive_when_the_source_id_parses()
    {
        var (competition, league, terminal, _, _) = QualifiedSlot(position: 9);
        competition.RemoveStage(league.Id, Clock);

        var place = ResolutionFreshnessDetector.Evaluate(competition, [terminal], Summaries(league))
            .SelectMany(pair => pair.Value)
            .Single(occupant => occupant.SlotKey == "Champ");
        place.Definitive.Should().BeTrue();
        ResolutionFreshnessDetector.Inspect(competition, [terminal], Summaries(league))
            .Single(occupant => occupant.SlotKey == "Champ")
            .SourceAbsence.Should().Be(ResolutionSourceAbsence.StageNotLoaded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_cancelled_leg_is_definitive_only_when_the_source_is_completed(bool completed)
    {
        var (competition, source, destination, match) = Progression(finish: false);
        match.Cancel(Clock);
        Move(source, completed ? StageStatus.Completed : StageStatus.Running);
        AssertProgression(competition, source, destination, [match], completed, StructureResolutionIndeterminacyCause.MatchNonJoue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Legs_that_were_never_generated_are_definitive_only_when_the_source_is_completed(bool completed)
    {
        var (competition, source, destination, _) = Progression(finish: false, attach: false);
        Move(source, completed ? StageStatus.Completed : StageStatus.Running);
        AssertProgression(competition, source, destination, [], completed, StructureResolutionIndeterminacyCause.MatchNonJoue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Extra_time_that_was_not_played_is_definitive_only_when_the_source_is_completed(bool completed)
    {
        var (competition, source, destination, match) = Progression(
            finish: true,
            draw: true,
            tie: new TieFormat(TieFormat.SingleLeg, aggregateScoring: false, extraTimeRule: new ExtraTimeRule()));
        Move(source, completed ? StageStatus.Completed : StageStatus.Running);
        AssertProgression(competition, source, destination, [match], completed, StructureResolutionIndeterminacyCause.Egalite);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_draw_without_a_shootout_is_definitive_only_when_the_source_is_completed(bool completed)
    {
        var (competition, source, destination, match) = Progression(finish: true, draw: true);
        Move(source, completed ? StageStatus.Completed : StageStatus.Running);
        AssertProgression(competition, source, destination, [match], completed, StructureResolutionIndeterminacyCause.Egalite);
    }

    [Fact]
    public void The_structure_read_reports_the_same_verdict_as_the_function()
    {
        var (competition, league, terminal, entries, match) = QualifiedSlot(position: 9);
        Move(league, StageStatus.Completed);
        var stages = new List<Stage> { league, terminal };
        var summaries = Summaries(league, match);
        var dto = ResolutionFreshnessDetector.Evaluate(competition, stages, summaries)
            .SelectMany(pair => pair.Value)
            .Single(occupant => occupant.SlotKey == "Champ");
        var assessment = ResolutionFreshnessDetector.Inspect(competition, stages, summaries)
            .Single(occupant => occupant.SlotKey == "Champ");
        var path = league.Regulation.QualificationRules!.Paths.Single();
        var verdict = ResolutionDefinitiveness.Evaluate(
            competition,
            league,
            path,
            assessment,
            terminal,
            []);

        dto.Definitive.Should().BeTrue();
        dto.Definitive.Should().Be(verdict.Definitive);
        dto.Releasable.Should().Be(verdict.Releasable);
        verdict.Releasable.Should().BeTrue();
        entries.Should().NotBeEmpty();
    }

    [Fact]
    public void Releasable_follows_the_release_guards_and_stays_false_when_matches_were_not_loaded()
    {
        var (competition, source, destination) = World(CompetitionStatus.Running, "Completed");
        var occupant = Occupant(destination, StructureResolutionIndeterminacyCause.SelectionVide, null, source!.Id.Value);
        var loaded = ResolutionDefinitiveness.Evaluate(
            competition, source, Path("Group", destination.Id), occupant, destination, []);
        var subject = new ResolutionReleaseSubject(
            occupant.Target,
            occupant.EntryId,
            occupant.Kind,
            occupant.State,
            occupant.IndeterminacyCause,
            occupant.SlotKey,
            occupant.GroupId);
        loaded.Releasable.Should().Be(ResolutionReleaseRules.Refusal(destination, subject, []) is null);
        loaded.RefusalCode.Should().Be(ResolutionReleaseRules.Refusal(destination, subject, []));

        var unloaded = ResolutionDefinitiveness.Evaluate(
            competition, source, Path("Group", destination.Id), occupant, destination, null);
        unloaded.Releasable.Should().BeFalse();
        unloaded.RefusalCode.Should().BeNull();
        unloaded.Definitive.Should().Be(loaded.Definitive);
    }

    private static bool Expected(
        StructureResolutionIndeterminacyCause cause,
        ResolutionSourceAbsence? absence,
        bool sourceCompleted,
        bool overall,
        CompetitionStatus competitionStatus,
        bool sourceIdInCompetition,
        Guid parsedId,
        Competition competition)
    {
        var idAbsent = !competition.StageIds.Any(id => id.Value == parsedId);
        return cause switch
        {
            StructureResolutionIndeterminacyCause.MatchNonJoue => sourceCompleted,
            StructureResolutionIndeterminacyCause.Egalite => sourceCompleted,
            StructureResolutionIndeterminacyCause.EgaliteClassement or StructureResolutionIndeterminacyCause.ConditionNonRemplie =>
                sourceCompleted && (!overall || competitionStatus is not (CompetitionStatus.Running or CompetitionStatus.Suspended)),
            StructureResolutionIndeterminacyCause.SelectionVide =>
                sourceCompleted && !(overall && !competition.Entries.Any(entry => entry.Status == EntryStatus.Active)),
            StructureResolutionIndeterminacyCause.SourceAbsente when absence == ResolutionSourceAbsence.Unreadable => false,
            StructureResolutionIndeterminacyCause.SourceAbsente when absence == ResolutionSourceAbsence.StageNotLoaded =>
                idAbsent && !sourceIdInCompetition,
            StructureResolutionIndeterminacyCause.SourceAbsente when absence == ResolutionSourceAbsence.PathGone => sourceCompleted,
            _ => false,
        };
    }

    private static bool JudgeStanding(
        StructureResolutionIndeterminacyCause cause,
        string scope,
        CompetitionStatus competitionStatus,
        StageStatus sourceStatus)
    {
        var (competition, source, destination) = World(competitionStatus, sourceStatus.ToString());
        return ResolutionDefinitiveness.Evaluate(
            competition,
            source,
            Path(scope, destination.Id),
            Occupant(destination, cause, null, source!.Id.Value),
            destination,
            []).Definitive;
    }

    private static void AssertProgression(
        Competition competition,
        Stage source,
        Stage destination,
        IReadOnlyList<Match> matches,
        bool definitive,
        StructureResolutionIndeterminacyCause cause)
    {
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [source.Id] = [.. matches.Select(Row)],
            [destination.Id] = [],
        };
        var place = ResolutionFreshnessDetector.Evaluate(competition, [source, destination], summaries)
            .SelectMany(pair => pair.Value)
            .Single(occupant => occupant.SlotKey == "Jouer");
        place.IndeterminacyCause.Should().Be(cause);
        place.Definitive.Should().Be(definitive);
    }

    private static (Competition Competition, Stage? Source, Stage Destination) World(
        CompetitionStatus competitionStatus,
        string sourceStatus)
    {
        var competition = CreateCompetition.Execute($"Definitive {Guid.NewGuid():N}", Clock);
        AddEntry.Execute(competition, "Nord", Clock);
        var destination = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), Clock);
        destination.AddSlot("Champ");
        competition.AddStage(destination.Id, Clock);
        Stage? source = null;
        if (sourceStatus != "Deleted")
        {
            source = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), Clock);
            source.AddMatchday(1, Clock);
            competition.AddStage(source.Id, Clock);
            Move(source, Enum.Parse<StageStatus>(sourceStatus));
        }

        MoveCompetition(competition, competitionStatus);
        return (competition, source, destination);
    }

    private static QualificationPath Path(string scope, StageId destinationId)
    {
        var source = scope switch
        {
            "Group" => QualificationSource.FromGroup(new GroupId(Guid.NewGuid())),
            "AcrossGroups" => QualificationSource.AcrossGroups(1),
            _ => QualificationSource.Overall(),
        };
        return new QualificationPath(
            1,
            source,
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForSlot(destinationId, "Champ"));
    }

    private static ResolutionOccupantAssessment Occupant(
        Stage destination,
        StructureResolutionIndeterminacyCause cause,
        ResolutionSourceAbsence? absence,
        Guid? parsedSourceId) =>
        new(
            destination.Id.Value,
            StructureResolutionOccupantTarget.Slot,
            Guid.NewGuid(),
            ResolutionProvenanceKind.Qualification,
            "path",
            StructureResolutionFreshness.Indetermine,
            null,
            "Champ",
            null,
            parsedSourceId,
            [],
            null,
            null,
            cause,
            null,
            null,
            absence,
            parsedSourceId);

    private static (Competition Competition, Stage League, Stage Terminal, EntryId[] Entries, Match Match) QualifiedSlot(int position)
    {
        var competition = CreateCompetition.Execute($"Slot {Guid.NewGuid():N}", Clock);
        var entries = new[]
        {
            AddEntry.Execute(competition, "Nord", Clock).Id,
            AddEntry.Execute(competition, "Sud", Clock).Id,
        };
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), Clock);
        league.AddMatchday(1, Clock);
        competition.AddStage(league.Id, Clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), Clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, Clock);
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            QualificationDestination.ForSlot(terminal.Id, "Champ"));
        league.ReplaceQualificationRules(new QualificationRules([path]), Clock);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(league.Id, path),
            "seed");
        terminal.AddResolvedPopulationEntry(entries[0], Clock, provenance);
        terminal.ApplyResolvedEntry("Champ", entries[0], Clock, provenance);
        var match = Match.Create(competition.Id, league.Id, entries[0], entries[1], Clock);
        match.Start(Clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), Clock);
        return (competition, league, terminal, entries, match);
    }

    private static (Competition Competition, Stage Source, Stage Destination, Match Match) Progression(
        bool finish,
        bool draw = false,
        bool attach = true,
        TieFormat? tie = null)
    {
        var competition = CreateCompetition.Execute($"Prog {Guid.NewGuid():N}", Clock);
        var home = AddEntry.Execute(competition, "Nord", Clock).Id;
        var away = AddEntry.Execute(competition, "Est", Clock).Id;
        var source = Stage.Create(competition.Id, new StageName("Barrage"), SampleRegulations.Standard(), Clock);
        source.AddRound("Tour", tie ?? new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), Clock);
        source.AddSlot("A");
        source.AddSlot("B");
        source.ReplaceBracketPairs([new BracketPair("P1", "A", "B")]);
        competition.AddStage(source.Id, Clock);
        source.AddResolvedPopulationEntry(home, Clock);
        source.AddResolvedPopulationEntry(away, Clock);
        source.AssignEntryToSlot("A", home);
        source.AssignEntryToSlot("B", away);
        var fixture = source.AddFixture(source.Rounds[0].Id, Clock, "A", "B", "P1");
        var match = Match.Create(competition.Id, source.Id, home, away, Clock);
        if (attach)
        {
            source.AttachMatch(fixture.Id, match.Id, 1, Clock);
        }

        if (finish)
        {
            match.Start(Clock);
            match.Finish(new MatchResult(ResultType.Played, new Score(draw ? 1 : 2, draw ? 1 : 0)), Clock);
        }

        var destination = Stage.Create(competition.Id, new StageName("Suite"), SampleRegulations.Standard(), Clock);
        destination.AddSlot("Jouer");
        competition.AddStage(destination.Id, Clock);
        var path = new ProgressionPath("P1", ProgressionOutcome.Winner, new ProgressionDestination(destination.Id, "Jouer"));
        source.ReplaceProgressionRules(new ProgressionRules([path]), Clock);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Progression,
            ResolutionPathIdentifier.ForProgression(source.Id, path),
            "seed");
        destination.AddResolvedPopulationEntry(home, Clock, provenance);
        destination.ApplyResolvedEntry("Jouer", home, Clock, provenance);
        return (competition, source, destination, match);
    }

    private static void Move(Stage stage, StageStatus status)
    {
        if (status == StageStatus.Draft)
        {
            return;
        }

        if (stage.Matchdays.Count == 0 && stage.Rounds.Count == 0 && stage.Groups.Count == 0)
        {
            stage.AddMatchday(1, Clock);
        }

        if (stage.Status == StageStatus.Draft)
        {
            stage.Prepare(Clock);
        }

        if (status == StageStatus.Ready)
        {
            return;
        }

        if (stage.Status == StageStatus.Ready)
        {
            stage.Start(Clock);
        }

        if (status == StageStatus.Running)
        {
            return;
        }

        if (status == StageStatus.Suspended)
        {
            stage.Suspend(Clock);
            return;
        }

        stage.Complete(Clock);
    }

    private static void MoveCompetition(Competition competition, CompetitionStatus status)
    {
        if (status == CompetitionStatus.Draft)
        {
            return;
        }

        if (competition.Status == CompetitionStatus.Draft)
        {
            competition.Prepare(Clock);
        }

        if (status == CompetitionStatus.Ready)
        {
            return;
        }

        if (competition.Status == CompetitionStatus.Ready)
        {
            competition.Start(Clock);
        }

        if (status == CompetitionStatus.Running)
        {
            return;
        }

        if (status == CompetitionStatus.Suspended)
        {
            competition.Suspend(Clock);
            return;
        }

        competition.Complete(CompletionMode.Normal, Clock);
    }

    private static Dictionary<StageId, IReadOnlyList<MatchSummaryRow>> Summaries(Stage stage, Match? match = null) =>
        new()
        {
            [stage.Id] = match is null ? [] : [Row(match)],
        };

    private static MatchSummaryRow Row(Match match) =>
        new(match.Id, match.StageId, match.Status, match.HomeEntryId, match.AwayEntryId, match.Result, match.HasObservedLive);
}

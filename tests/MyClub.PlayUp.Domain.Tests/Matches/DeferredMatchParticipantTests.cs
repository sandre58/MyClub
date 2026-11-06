// -----------------------------------------------------------------------
// <copyright file="DeferredMatchParticipantTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Matches.Events;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Matches;

public sealed class DeferredMatchParticipantTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly StageId _stageId = StageId.New();

    [Fact]
    public void Create_allows_one_unknown_side_and_rejects_same_participant_only_when_both_are_known()
    {
        var known = EntryId.New();

        var homeUnknown = Match.Create(_competitionId, _stageId, null, known, _clock);
        homeUnknown.HomeEntryId.Should().BeNull();
        homeUnknown.AwayEntryId.Should().Be(known);
        homeUnknown.ParticipantsResolved.Should().BeFalse();

        var bothUnknown = Match.Create(_competitionId, _stageId, null, null, _clock);
        bothUnknown.ParticipantsResolved.Should().BeFalse();

        var act = () => Match.Create(_competitionId, _stageId, known, known, _clock);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SameParticipant);
    }

    [Fact]
    public void ResolveParticipant_fills_a_null_side_emits_the_event_and_refuses_overwrite()
    {
        var away = EntryId.New();
        var home = EntryId.New();
        var other = EntryId.New();
        var match = Match.Create(_competitionId, _stageId, null, away, _clock);
        match.ClearDomainEvents();

        match.ResolveParticipant(Side.Home, home, _clock);

        match.HomeEntryId.Should().Be(home);
        match.ParticipantsResolved.Should().BeTrue();
        var resolved = match.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MatchParticipantResolved>().Subject;
        resolved.Side.Should().Be(Side.Home);
        resolved.EntryId.Should().Be(home);

        match.ClearDomainEvents();
        match.ResolveParticipant(Side.Home, home, _clock);
        match.DomainEvents.Should().BeEmpty();

        var overwrite = () => match.ResolveParticipant(Side.Home, other, _clock);
        overwrite.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipantAlreadyResolved);
        match.HomeEntryId.Should().Be(home);
    }

    [Fact]
    public void ResolveParticipant_rejects_the_other_side_entry()
    {
        var away = EntryId.New();
        var match = Match.Create(_competitionId, _stageId, null, away, _clock);

        var act = () => match.ResolveParticipant(Side.Home, away, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.SameParticipant);
        match.HomeEntryId.Should().BeNull();
    }

    [Fact]
    public void Start_finish_and_correct_refuse_while_a_side_is_null()
    {
        var match = Match.Create(_competitionId, _stageId, null, EntryId.New(), _clock);
        var result = new MatchResult(ResultType.Played, new Score(1, 0));

        var start = () => match.Start(_clock);
        var finish = () => match.Finish(result, _clock);
        var correct = () => match.CorrectMatchResult(result, _clock);
        start.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipantsUnresolved);
        finish.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipantsUnresolved);
        correct.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.ParticipantsUnresolved);
    }

    [Fact]
    public void Start_and_finish_succeed_once_both_sides_are_known()
    {
        var match = Match.Create(_competitionId, _stageId, null, EntryId.New(), _clock);
        match.ResolveParticipant(Side.Home, EntryId.New(), _clock);

        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);

        match.Status.Should().Be(MatchStatus.Finished);
        var correct = () => match.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(3, 1)), _clock);
        correct.Should().NotThrow();
    }

    [Fact]
    public void Cup_coherence_compares_known_sides_only()
    {
        var stage = Stage.Create(_competitionId, new StageName("Demi"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("SF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF-A");
        stage.AddSlot("SF-B");
        stage.SeedEntryRoundBracketPairs();
        var pair = stage.BracketPairs.Should().ContainSingle().Subject;
        var away = EntryId.New();
        var home = EntryId.New();
        var fixture = stage.AddFixture(round.Id, _clock, pair.SlotAKey, pair.SlotBKey, pair.PairKey);
        var match = Match.Create(_competitionId, stage.Id, null, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        var snapshots = new[] { new MatchParticipantSnapshot(match.Id, null, away) };
        var proposed = MaterializedOpponentCoherence.CopySlotOccupants(stage);
        proposed[pair.SlotAKey] = home;
        proposed[pair.SlotBKey] = away;

        var accept = () => MaterializedOpponentCoherence.EnsureCupPairsCoherent(stage, proposed, snapshots);
        accept.Should().NotThrow();

        proposed[pair.SlotBKey] = EntryId.New();
        var diverge = () => MaterializedOpponentCoherence.EnsureCupPairsCoherent(stage, proposed, snapshots);
        diverge.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
    }
}

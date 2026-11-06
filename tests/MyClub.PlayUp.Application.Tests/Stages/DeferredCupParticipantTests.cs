// -----------------------------------------------------------------------
// <copyright file="DeferredCupParticipantTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Matches.Events;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class DeferredCupParticipantTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 5, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Materialize_then_apply_fills_one_side_then_the_other_without_conflict()
    {
        var competition = CreateCompetition.Execute("T5-apply", _clock);
        var home = competition.AddEntry(TeamId.New(), "Alpha", _clock).Id;
        var away = competition.AddEntry(TeamId.New(), "Beta", _clock).Id;
        var league = Stage.Create(competition.Id, new StageName("Poule"), SampleRegulations.Standard(), _clock);
        var cup = Stage.Create(competition.Id, new StageName("Demi"), SampleRegulations.Standard(), _clock);
        competition.AddStage(league.Id, _clock);
        competition.AddStage(cup.Id, _clock);
        league.AddMatchday(1, _clock);
        var played = Match.Create(competition.Id, league.Id, home, away, _clock);
        played.Start(_clock);
        played.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);

        cup.AddRound("SF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        cup.AddSlot("SF-A");
        cup.AddSlot("SF-B");
        cup.SeedEntryRoundBracketPairs();
        PrepareStage.Execute(cup, [league, cup], _clock);

        var created = MaterializeCupFromOccupiedSlots.Execute(competition, cup, ["P1"], [], _clock);
        var match = created.CreatedMatches.Should().ContainSingle().Subject;
        match.HomeEntryId.Should().BeNull();
        match.AwayEntryId.Should().BeNull();

        var standing = CalculateStanding.Execute(
            [home, away],
            [played],
            competition.Regulation.StandingRules);
        var stages = new[] { league, cup };
        league.ReplaceQualificationRules(
            new QualificationRules([PathTo(1, 1, cup, "SF-A")]),
            _clock);

        var first = () => ApplyQualification.Execute(
            league,
            standing,
            new Dictionary<GroupId, Standing>(),
            matches: null,
            stages,
            _clock,
            destinationMatches: [match]);
        first.Should().NotThrow();
        match.HomeEntryId.Should().Be(home);
        match.AwayEntryId.Should().BeNull();
        match.DomainEvents.OfType<MatchParticipantResolved>().Should().ContainSingle();

        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                PathTo(1, 1, cup, "SF-A"),
                PathTo(2, 2, cup, "SF-B")
            ]),
            _clock);

        var second = () => ApplyQualification.Execute(
            league,
            standing,
            new Dictionary<GroupId, Standing>(),
            matches: null,
            stages,
            _clock,
            destinationMatches: [match]);
        second.Should().NotThrow();
        match.HomeEntryId.Should().Be(home);
        match.AwayEntryId.Should().Be(away);
        match.DomainEvents.OfType<MatchParticipantResolved>().Should().HaveCount(2);
    }

    private static QualificationPath PathTo(int order, int position, Stage cup, string slotKey) =>
        new(
            order,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            QualificationDestination.ForSlot(cup.Id, slotKey));
}

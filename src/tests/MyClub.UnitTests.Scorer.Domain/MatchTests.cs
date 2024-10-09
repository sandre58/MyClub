// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using Xunit;

namespace MyClub.UnitTests.Scorer.Domain
{
    public class MatchTests
    {
        private readonly IVirtualTeam _homeTeam;
        private readonly IVirtualTeam _awayTeam;
        private readonly Match _match;

        public MatchTests()
        {
            _homeTeam = new Team("Home Team");
            _awayTeam = new Team("Away Team");
            _match = new Match(DateTime.Now, _homeTeam, _awayTeam);
        }

        [Fact]
        public void TestMatchInitialization()
        {
            Assert.Equal(_homeTeam, _match.HomeTeam);
            Assert.Equal(_awayTeam, _match.AwayTeam);
        }

        [Fact]
        public void TestPostponeMatch()
        {
            var newDate = DateTime.Now.AddDays(7);
            _match.Postpone(newDate);
            Assert.Equal(newDate, _match.PostponedDate);
            Assert.Equal(newDate, _match.Date);
        }

        [Fact]
        public void TestScheduleMatch()
        {
            var newDate = DateTime.Now.AddDays(7);
            _match.Schedule(newDate);
            Assert.Equal(newDate, _match.OriginDate);
        }

        [Fact]
        public void TestSetScore()
        {
            _match.SetScore(2, 1);
            Assert.Equal(2, _match.Home!.GetScore());
            Assert.Equal(1, _match.Away!.GetScore());
        }

        [Fact]
        public void TestInvertTeams()
        {
            _match.Invert();
            Assert.Equal(_homeTeam, _match.Away!.Team);
            Assert.Equal(_awayTeam, _match.Home!.Team);
        }

        [Fact]
        public void TestGetResultOf()
        {
            _match.SetScore(2, 1);
            Assert.Equal(Result.Won, _match.GetResultOf(_homeTeam.Id));
            Assert.Equal(Result.Lost, _match.GetResultOf(_awayTeam.Id));
        }

        [Fact]
        public void TestIsDraw()
        {
            _match.SetScore(1, 1);
            Assert.True(_match.IsDraw());
        }

        [Fact]
        public void TestCancelMatch()
        {
            _match.Cancel();
            Assert.Equal(MatchState.Cancelled, _match.State);
        }

        // Additional tests can be added here for other methods and scenarios
    }
}

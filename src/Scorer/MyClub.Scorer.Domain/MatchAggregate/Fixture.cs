// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class Fixture : AuditableEntity, IMatchesProvider, IFixture
    {
        private readonly WinnerTeam<Fixture> _winnerTeam;
        private readonly LooserTeam<Fixture> _looserTeam;

        public Fixture(RoundOfFixtures stage, IVirtualTeam team1, IVirtualTeam team2, Guid? id = null) : base(id)
        {
            Stage = stage;
            Team1 = team1;
            Team2 = team2;
            _winnerTeam = new(this);
            _looserTeam = new(this);
        }

        public RoundOfFixtures Stage { get; }

        public IVirtualTeam Team1 { get; }

        public IVirtualTeam Team2 { get; }

        public bool IsPlayed() => GetAllMatches().All(x => x.State is MatchState.Played or MatchState.Cancelled);

        public ExtendedResult GetExtendedResultOf(Guid teamId) => Stage.ResultStrategy.GetExtendedResultOf(this, teamId);

        public Result GetResultOf(Guid teamId) => Stage.ResultStrategy.GetResultOf(this, teamId);

        public Team? GetWinner()
            => Team1.GetTeam() is not Team team1 || Team2.GetTeam() is not Team team2
                ? null
                : GetResultOf(team1.Id) switch
                {
                    Result.Won => team1,
                    Result.Lost => team2,
                    _ => null,
                };

        public Team? GetLooser()
            => Team1.GetTeam() is not Team team1 || Team2.GetTeam() is not Team team2
                ? null
                : GetResultOf(team1.Id) switch
                {
                    Result.Won => team2,
                    Result.Lost => team1,
                    _ => null,
                };

        public bool Participate(Guid teamId) => GetTeams().Select(x => x.Id).Contains(teamId);

        private IEnumerable<IVirtualTeam> GetTeams() => new List<IVirtualTeam?>() { Team1, Team2, Team1.GetTeam(), Team2.GetTeam() }.NotNull().Distinct();

        public IEnumerable<Match> GetAllMatches() => Stage.GetAllMatches().Where(x => x.Participate(Team1.Id) && x.Participate(Team2.Id));

        public IVirtualTeam GetWinnerTeam() => _winnerTeam;

        public IVirtualTeam GetLooserTeam() => _looserTeam;

        public override string ToString() => $"{Team1} vs {Team2}";
    }
}

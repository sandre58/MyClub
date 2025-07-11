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
        private readonly WinnerOfFixtureTeam _winnerTeam;
        private readonly LooserOfFixtureTeam _looserTeam;

        public Fixture(Round stage, IVirtualTeam team1, IVirtualTeam team2, int? rank = null, Guid? id = null) : base(id)
        {
            Rank = rank;
            Stage = stage;
            Team1 = team1;
            Team2 = team2;
            _winnerTeam = new(this);
            _looserTeam = new(this);
        }

        public Round Stage { get; }

        public IVirtualTeam Team1 { get; }

        public IVirtualTeam Team2 { get; }

        public int? Rank { get; }

        public bool IsPlayed() => GetAllMatches().All(x => x.State is MatchState.Played or MatchState.Cancelled);

        public bool IsDraw() => HasResult() && GetWinner() is null;

        public bool HasResult() => GetAllMatches().Where(x => x.State is not MatchState.Cancelled).All(x => x.HasResult());

        public ExtendedResult GetExtendedResultOf(IVirtualTeam team) => Stage.GetExtendedResultOf(team);

        public ExtendedResult GetExtendedResultOf(Guid teamId) => GetTeam(teamId) is IVirtualTeam team ? Stage.GetExtendedResultOf(team) : ExtendedResult.None;

        public Result GetResultOf(IVirtualTeam team) => Stage.GetResultOf(team);

        public Result GetResultOf(Guid teamId) => GetTeam(teamId) is IVirtualTeam team ? GetResultOf(team) : Result.None;

        public Team? GetWinner()
            => Team1.GetTeam() is Team team1 && Team2.GetTeam() is Team team2
                ? Stage.GetResultOf(team1) switch
                {
                    Result.Won => team1,
                    Result.Lost => team2,
                    _ => null,
                }
                : null;

        public Team? GetLooser()
            => Team1.GetTeam() is Team team1 && Team2.GetTeam() is Team team2
                ? Stage.GetResultOf(team1) switch
                {
                    Result.Won => team2,
                    Result.Lost => team1,
                    _ => null,
                }
                : null;

        public bool IsWonBy(IVirtualTeam team) => GetResultOf(team) == Result.Won;

        public bool IsWonBy(Guid teamId) => GetTeam(teamId) is IVirtualTeam team && IsWonBy(team);

        public bool IsLostBy(IVirtualTeam team) => GetResultOf(team) == Result.Lost;

        public bool IsLostBy(Guid teamId) => GetTeam(teamId) is IVirtualTeam team && IsLostBy(team);

        public bool Participate(IVirtualTeam team) => GetTeams().Contains(team);

        public bool Participate(Guid teamId) => GetTeam(teamId) is IVirtualTeam team && Participate(team);

        private IEnumerable<IVirtualTeam> GetTeams() => new List<IVirtualTeam?>() { Team1, Team2, Team1.GetTeam(), Team2.GetTeam(), _winnerTeam, _looserTeam }.NotNull().Distinct();

        private IVirtualTeam? GetTeam(Guid teamId) => GetTeams().GetById(teamId);

        public IEnumerable<Match> GetAllMatches() => Stage.GetAllMatches().Where(x => x.Participate(Team1) && x.Participate(Team2));

        public IVirtualTeam GetWinnerTeam() => _winnerTeam;

        public IVirtualTeam GetLooserTeam() => _looserTeam;

        public override string ToString() => $"{Team1} vs {Team2}";
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Collections;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public abstract class Championship : AuditableEntity
    {
        private readonly ExtendedObservableCollection<ITeam> _teams = [];
        private readonly Dictionary<ITeam, int> _penaltyPoints = [];

        protected Championship(Guid? id = null) : base(id) => Teams = new(_teams);

        public ReadOnlyObservableCollection<ITeam> Teams { get; }

        public Dictionary<AcceptableValueRange<int>, RankLabel> Labels { get; } = [];

        public abstract IEnumerable<Match> GetMatches();

        public abstract RankingRules GetRankingRules();

        #region Ranking

        public Ranking GetRanking() => new(Teams, GetMatches(), GetRankingRules(), GetPenaltyPoints(), Labels, (x, y) => x.State is MatchState.Played);

        public Ranking GetHomeRanking() => new(Teams, GetMatches(), GetRankingRules(), null, null, (x, y) => y.Equals(x.HomeTeam) && x.State == MatchState.Played);

        public Ranking GetAwayRanking() => new(Teams, GetMatches(), GetRankingRules(), null, null, (x, y) => y.Equals(x.AwayTeam) && x.State == MatchState.Played);

        public Ranking GetLiveRanking() => new(Teams, GetMatches(), GetRankingRules(), GetPenaltyPoints(), Labels, (x, y) => x.State is MatchState.Played or MatchState.InProgress);

        #endregion

        #region Penalty

        public virtual IReadOnlyDictionary<ITeam, int> GetPenaltyPoints() => _penaltyPoints;

        public virtual void AddPenalty(ITeam team, int points) => _ = _penaltyPoints.AddOrUpdate(team, points);

        public virtual void AddPenalty(Guid teamId, int points) => _ = _penaltyPoints.AddOrUpdate(Teams.GetById(teamId), points);

        public virtual bool RemovePenalty(ITeam team) => _penaltyPoints.Remove(team);

        public virtual void ClearPenaltyPoints() => _penaltyPoints.Clear();

        #endregion

        #region Teams

        public Team AddTeam(Team team)
        {
            if (Teams.Contains(team))
                throw new AlreadyExistsException(nameof(Teams), team);

            _teams.Add(team);

            return team;
        }

        public virtual bool RemoveTeam(Team team)
        {
            _penaltyPoints.Remove(team);
            return _teams.Remove(team);
        }

        #endregion
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    public abstract class Championship : AuditableEntity, IMatchesProvider, ITeamsProvider
    {
        private readonly ExtendedObservableCollection<IVirtualTeam> _teams = [];
        private readonly Dictionary<IVirtualTeam, int> _penaltyPoints = [];

        protected Championship(Guid? id = null) : base(id) => Teams = new(_teams);

        public ReadOnlyObservableCollection<IVirtualTeam> Teams { get; }

        public Dictionary<AcceptableValueRange<int>, RankLabel> Labels { get; } = [];

        public abstract IEnumerable<Match> GetAllMatches();

        public abstract RankingRules GetRankingRules();

        IEnumerable<IVirtualTeam> ITeamsProvider.ProvideTeams() => Teams.AsEnumerable();

        #region Ranking

        public Ranking GetRanking() => new(Teams, GetAllMatches(), GetRankingRules(), GetPenaltyPoints(), Labels, (x, y) => x.State is MatchState.Played);

        public Ranking GetHomeRanking() => new(Teams, GetAllMatches(), GetRankingRules(), null, null, (x, y) => y.Equals(x.HomeTeam) && x.State == MatchState.Played);

        public Ranking GetAwayRanking() => new(Teams, GetAllMatches(), GetRankingRules(), null, null, (x, y) => y.Equals(x.AwayTeam) && x.State == MatchState.Played);

        public Ranking GetLiveRanking() => new(Teams, GetAllMatches(), GetRankingRules(), GetPenaltyPoints(), Labels, (x, y) => x.State is MatchState.Played or MatchState.InProgress);

        #endregion

        #region Penalty

        public virtual IReadOnlyDictionary<IVirtualTeam, int> GetPenaltyPoints() => _penaltyPoints;

        public virtual void AddPenalty(IVirtualTeam team, int points) => _ = _penaltyPoints.AddOrUpdate(team, points);

        public virtual void AddPenalty(Guid teamId, int points) => _ = _penaltyPoints.AddOrUpdate(Teams.GetById(teamId), points);

        public virtual bool RemovePenalty(IVirtualTeam team) => _penaltyPoints.Remove(team);

        public virtual void ClearPenaltyPoints() => _penaltyPoints.Clear();

        #endregion

        #region Teams

        public IVirtualTeam AddTeam(IVirtualTeam team)
        {
            if (Teams.Contains(team))
                throw new AlreadyExistsException(nameof(Teams), team);

            _teams.Add(team);

            return team;
        }

        public virtual bool RemoveTeam(IVirtualTeam team)
        {
            _penaltyPoints.Remove(team);
            return _teams.Remove(team);
        }

        #endregion
    }
}

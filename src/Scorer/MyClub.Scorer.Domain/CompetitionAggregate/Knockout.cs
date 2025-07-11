// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Collections;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public abstract class Knockout : AuditableEntity, IStage
    {
        private readonly OptimizedObservableCollection<Round> _rounds = [];
        private readonly OptimizedObservableCollection<IVirtualTeam> _teams = [];

        protected Knockout(MatchFormat? matchFormat = null, MatchRules? matchRules = null, SchedulingParameters? schedulingParameters = null, Guid? id = null) : base(id)
        {
            Rounds = new(_rounds);
            Teams = new(_teams);
            MatchRules = matchRules ?? MatchRules.Default;
            MatchFormat = matchFormat ?? MatchFormat.NoDraw;
            SchedulingParameters = schedulingParameters ?? SchedulingParameters.Default;
        }

        public MatchFormat MatchFormat { get; set; }

        public MatchRules MatchRules { get; set; }

        public SchedulingParameters SchedulingParameters { get; set; }

        public ReadOnlyObservableCollection<Round> Rounds { get; }

        public ReadOnlyObservableCollection<IVirtualTeam> Teams { get; }

        public IEnumerable<Match> GetAllMatches() => Rounds.SelectMany(x => x.GetAllMatches());

        public IEnumerable<T> GetStages<T>() where T : IStage => Rounds.OfType<T>().Union(Rounds.SelectMany(x => x.GetStages<T>()));

        public bool RemoveMatch(Match item) => _rounds.Any(x => x.RemoveMatch(item));

        #region Rounds

        public Round AddRound(Round round)
        {
            if (Rounds.Contains(round))
                throw new AlreadyExistsException(nameof(Rounds), round);

            _rounds.Add(round);

            return round;
        }

        public bool RemoveRound(Round item) => _rounds.Remove(item);

        public void Clear() => _rounds.Clear();

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
            _rounds.ForEach(x => x.RemoveTeam(team));
            return _teams.Remove(team);
        }

        #endregion
    }
}


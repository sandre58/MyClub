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
        private readonly ObservableCollection<IRound> _rounds = [];
        private readonly ExtendedObservableCollection<IVirtualTeam> _teams = [];

        protected Knockout(Guid? id = null) : base(id)
        {
            Rounds = new(_rounds);
            Teams = new(_teams);
        }

        public ReadOnlyObservableCollection<IRound> Rounds { get; }

        public ReadOnlyObservableCollection<IVirtualTeam> Teams { get; }

        public abstract MatchRules ProvideRules();

        public abstract MatchFormat ProvideFormat();

        public abstract SchedulingParameters ProvideSchedulingParameters();

        IEnumerable<IVirtualTeam> ITeamsProvider.ProvideTeams() => Teams.AsEnumerable();

        public IEnumerable<Match> GetAllMatches() => Rounds.SelectMany(x => x.GetAllMatches());
        public IEnumerable<T> GetStages<T>() where T : ICompetitionStage => Rounds.OfType<T>().Union(Rounds.SelectMany(x => x.GetStages<T>()));

        public bool RemoveMatch(Match item) => _rounds.Any(x => x.RemoveMatch(item));

        #region Rounds

        public IRound AddRoundOfFixtures(string name, string? shortName = null) => AddRound(new RoundOfFixtures(this, name, shortName));

        public IRound AddRoundOfMatches(DateTime date, string name, string? shortName = null) => AddRound(new RoundOfMatches(this, date, name, shortName));

        public IRound AddRound(IRound round)
        {
            if (Rounds.Contains(round))
                throw new AlreadyExistsException(nameof(Rounds), round);

            _rounds.Add(round);

            return round;
        }

        public bool RemoveRound(IRound item) => _rounds.Remove(item);

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


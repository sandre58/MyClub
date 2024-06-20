// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities.Collections;
using MyNet.Utilities.Extensions;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class GroupStage : AuditableEntity, IStage, IMatchdaysProvider
    {
        private string _name = string.Empty;
        private readonly ExtendedObservableCollection<ITeam> _teams = [];
        private readonly ExtendedObservableCollection<Group> _groups = [];
        private readonly ExtendedObservableCollection<Matchday> _matchdays = [];

        public GroupStage(string name, IStage? parent = null, RankingRules? rankingRules = null, MatchFormat? matchFormat = null, Guid? id = null) : base(id)
        {
            Parent = parent;
            Name = name;
            RankingRules = rankingRules ?? RankingRules.Default;
            MatchFormat = matchFormat ?? MatchFormat.Default;
            Teams = new(_teams);
            Groups = new(_groups);
            Matchdays = new(_matchdays);
        }

        public string Name
        {
            get => _name;
            set => _name = value.IsRequiredOrThrow();
        }

        public RankingRules RankingRules { get; set; }

        public MatchFormat MatchFormat { get; set; }

        public ReadOnlyObservableCollection<ITeam> Teams { get; }

        public ReadOnlyObservableCollection<Group> Groups { get; }

        public ReadOnlyObservableCollection<Matchday> Matchdays { get; }

        public IStage? Parent { get; }

        public IEnumerable<Match> GetAllMatches() => Groups.SelectMany(x => x.GetAllMatches());

        MatchFormat IMatchFormatProvider.ProvideFormat() => MatchFormat;

        #region Matchdays

        public Matchday AddMatchday(DateTime date, string name, string? shortName = null) => AddMatchday(new Matchday(this, date, name, shortName));

        public Matchday AddMatchday(Matchday matchday)
        {
            if (Matchdays.Contains(matchday))
                throw new AlreadyExistsException(nameof(Matchdays), matchday);

            _matchdays.Add(matchday);

            return matchday;
        }

        public bool RemoveMatchday(Matchday item) => _matchdays.Remove(item);

        public void Clear() => _matchdays.Clear();

        #endregion
    }
}


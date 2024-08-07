﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Collections;
using MyNet.Utilities.Extensions;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class ChampionshipStage : Championship, IStage, IMatchdaysProvider
    {
        private string _name = string.Empty;
        private readonly ExtendedObservableCollection<Matchday> _matchdays = [];

        public ChampionshipStage(IStage parent, string name, RankingRules? rankingRules = null, MatchFormat? matchFormat = null, Guid? id = null) : base(id)
        {
            Parent = parent;
            SchedulingParameters = parent.ProvideSchedulingParameters();
            Name = name;
            RankingRules = rankingRules ?? RankingRules.Default;
            MatchFormat = matchFormat ?? MatchFormat.Default;
            Matchdays = new(_matchdays);
        }

        public ChampionshipStage(string name, SchedulingParameters schedulingParameters, RankingRules? rankingRules = null, MatchFormat? matchFormat = null, Guid? id = null) : base(id)
        {
            SchedulingParameters = schedulingParameters;
            Name = name;
            RankingRules = rankingRules ?? RankingRules.Default;
            MatchFormat = matchFormat ?? MatchFormat.Default;
            Matchdays = new(_matchdays);
        }

        public string Name
        {
            get => _name;
            set => _name = value.IsRequiredOrThrow();
        }

        public IStage? Parent { get; }

        public RankingRules RankingRules { get; set; }

        public MatchFormat MatchFormat { get; set; }

        public SchedulingParameters SchedulingParameters { get; set; }

        public ReadOnlyObservableCollection<Matchday> Matchdays { get; }

        public override RankingRules GetRankingRules() => RankingRules;

        public override IEnumerable<Match> GetAllMatches() => Matchdays.SelectMany(x => x.Matches);

        MatchFormat IMatchFormatProvider.ProvideFormat() => MatchFormat;

        SchedulingParameters ISchedulingParametersProvider.ProvideSchedulingParameters() => SchedulingParameters;

        public override bool RemoveTeam(Team team)
        {
            _matchdays.ForEach(x => x.Matches.Where(x => x.Participate(team)).ToList().ForEach(y => x.RemoveMatch(y)));
            return base.RemoveTeam(team);
        }

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

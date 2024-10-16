// Copyright (c) Stéphane ANDRE. All Right Reserved.
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
    public class ChampionshipStage : Championship, ITournamentStage, IMatchdaysStage
    {
        private string _name = string.Empty;
        private string _shortName = string.Empty;
        private readonly OptimizedObservableCollection<Matchday> _matchdays = [];

        public ChampionshipStage(IStage stage, string name, string? shortName = null, RankingRules? rankingRules = null, MatchFormat? matchFormat = null, MatchRules? matchRules = null, SchedulingParameters? schedulingParameters = null, Guid? id = null) : base(id)
        {
            Stage = stage;
            Name = name;
            ShortName = shortName ?? name.GetInitials();
            RankingRules = rankingRules ?? RankingRules.Default;
            MatchFormat = matchFormat ?? stage.ProvideFormat();
            MatchRules = matchRules ?? stage.ProvideRules();
            SchedulingParameters = schedulingParameters ?? stage.ProvideSchedulingParameters();
            Matchdays = new(_matchdays);
        }

        public string Name
        {
            get => _name;
            set => _name = value.IsRequiredOrThrow();
        }

        public string ShortName
        {
            get => _shortName;
            set => _shortName = value.IsRequiredOrThrow();
        }

        public IStage Stage { get; }

        public RankingRules RankingRules { get; set; }

        public MatchFormat MatchFormat { get; set; }

        public MatchRules MatchRules { get; set; }

        public SchedulingParameters SchedulingParameters { get; set; }

        public ReadOnlyObservableCollection<Matchday> Matchdays { get; }

        public override RankingRules GetRankingRules() => RankingRules;

        MatchFormat IMatchFormatProvider.ProvideFormat() => MatchFormat;

        MatchRules IMatchRulesProvider.ProvideRules() => MatchRules;

        SchedulingParameters ISchedulingParametersProvider.ProvideSchedulingParameters() => SchedulingParameters;

        public override IEnumerable<Match> GetAllMatches() => Matchdays.SelectMany(x => x.Matches);
        public IEnumerable<T> GetStages<T>() where T : ICompetitionStage => Matchdays.OfType<T>();

        public override bool RemoveTeam(IVirtualTeam team)
        {
            _matchdays.ForEach(x => x.Matches.Where(x => x.Participate(team)).ToList().ForEach(y => x.RemoveMatch(y)));
            return base.RemoveTeam(team);
        }

        public bool RemoveMatch(Match item) => _matchdays.Any(x => x.RemoveMatch(item));

        #region Matchdays

        public Matchday AddMatchday(DateTime date, string name, string? shortName = null) => AddMatchday(new Matchday(this, date, name, shortName));

        public Matchday AddMatchday(Matchday matchday)
        {
            if (!ReferenceEquals(matchday.Stage, this))
                throw new ArgumentException("Matchday stage is not this stage", nameof(matchday));

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

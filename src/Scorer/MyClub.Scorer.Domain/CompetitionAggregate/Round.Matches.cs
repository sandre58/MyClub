// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities.Collections;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class RoundOfMatches : MatchesStage<MatchOfRound>, IRound
    {
        private readonly OptimizedObservableCollection<IVirtualTeam> _teams = [];

        public RoundOfMatches(Knockout stage,
                              DateTime date,
                              string name,
                              string? shortName = null,
                              MatchFormat? matchFormat = null,
                              MatchRules? matchRules = null,
                              SchedulingParameters? schedulingParameters = null,
                              Guid? id = null)
            : base(date, name, shortName, id)
        {
            Stage = stage;
            MatchFormat = matchFormat ?? stage.ProvideFormat();
            MatchRules = matchRules ?? stage.ProvideRules();
            SchedulingParameters = schedulingParameters ?? stage.ProvideSchedulingParameters();
            Teams = new(_teams);
        }

        public Knockout Stage { get; }

        public MatchFormat MatchFormat { get; set; }

        public MatchRules MatchRules { get; set; }

        public SchedulingParameters SchedulingParameters { get; set; }

        public ReadOnlyObservableCollection<IVirtualTeam> Teams { get; }

        public override MatchFormat ProvideFormat() => MatchFormat;

        public override MatchRules ProvideRules() => MatchRules;

        public override SchedulingParameters ProvideSchedulingParameters() => SchedulingParameters;

        public override IEnumerable<IVirtualTeam> ProvideTeams() => Teams.AsEnumerable();

        protected override MatchOfRound Create(DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam) => new(this, date, homeTeam, awayTeam);

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
            Matches.Where(x => x.Participate(team)).ToList().ForEach(y => RemoveMatch(y));
            return _teams.Remove(team);
        }

        #endregion
    }
}

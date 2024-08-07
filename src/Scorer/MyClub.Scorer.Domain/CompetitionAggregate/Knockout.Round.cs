﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities.Collections;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public abstract class Round : NameEntity, IMatchFormatProvider, ISchedulingParametersProvider
    {
        private readonly ExtendedObservableCollection<ITeam> _teams = [];
        private readonly ExtendedObservableCollection<Fixtures> _fixtures = [];

        protected Round(string name, string shortName, MatchFormat? matchFormat = null, SchedulingParameters? schedulingParameters = null, Guid? id = null) : base(name, shortName, id)
        {
            MatchFormat = matchFormat ?? MatchFormat.NoDraw;
            SchedulingParameters = schedulingParameters ?? SchedulingParameters.Default;
            Teams = new(_teams);
            Fixtures = new(_fixtures);
        }

        public MatchFormat MatchFormat { get; set; }

        public SchedulingParameters SchedulingParameters { get; set; }

        public ReadOnlyObservableCollection<ITeam> Teams { get; }

        public ReadOnlyObservableCollection<Fixtures> Fixtures { get; }

        MatchFormat IMatchFormatProvider.ProvideFormat() => MatchFormat;

        SchedulingParameters ISchedulingParametersProvider.ProvideSchedulingParameters() => SchedulingParameters;

        public IEnumerable<Match> GetAllMatches() => Fixtures.SelectMany(x => x.Matches);
    }
}

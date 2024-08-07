﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities.Collections;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Tournament : AuditableEntity, ICompetition
    {
        private readonly ExtendedObservableCollection<IStage> _stages = [];

        public Tournament() : this(SchedulingParameters.Default) { }

        public Tournament(SchedulingParameters schedulingParameters)
        {
            Stages = new(_stages);
            SchedulingParameters = schedulingParameters;
        }

        public ReadOnlyObservableCollection<IStage> Stages { get; }

        public MatchFormat MatchFormat { get; set; } = MatchFormat.NoDraw;

        public SchedulingParameters SchedulingParameters { get; set; }

        MatchFormat IMatchFormatProvider.ProvideFormat() => MatchFormat;

        SchedulingParameters ISchedulingParametersProvider.ProvideSchedulingParameters() => SchedulingParameters;

        public IEnumerable<IMatchdaysProvider> GetAllMatchdaysProviders() => Stages.OfType<IMatchdaysProvider>();

        public IEnumerable<IMatchesProvider> GetAllMatchesProviders() => GetAllMatchdaysProviders().SelectMany(x => x.Matchdays.OfType<IMatchesProvider>()).Union(Stages.OfType<Knockout>().SelectMany(x => x.Rounds).SelectMany(x => x.Fixtures));
    }
}


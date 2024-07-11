// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Cup : Knockout, ICompetition
    {
        public Cup() : this(SchedulingParameters.Default) { }

        public Cup(SchedulingParameters schedulingParameters) => SchedulingParameters = schedulingParameters;

        public MatchFormat MatchFormat { get; set; } = MatchFormat.NoDraw;

        public SchedulingParameters SchedulingParameters { get; set; }

        MatchFormat IMatchFormatProvider.ProvideFormat() => MatchFormat;

        SchedulingParameters ISchedulingParametersProvider.ProvideSchedulingParameters() => SchedulingParameters;

        public IEnumerable<IMatchdaysProvider> GetAllMatchdaysProviders() => [];

        public IEnumerable<IMatchesProvider> GetAllMatchesProviders() => Rounds.SelectMany(x => x.Fixtures);
    }
}


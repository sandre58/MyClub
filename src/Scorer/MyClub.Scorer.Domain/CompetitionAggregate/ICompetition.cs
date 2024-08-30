// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface ICompetition : IEntity, IAggregateRoot, IMatchFormatProvider, ISchedulingParametersProvider
    {
        MatchFormat MatchFormat { get; set; }

        SchedulingParameters SchedulingParameters { get; set; }

        IEnumerable<IMatchdaysProvider> GetAllMatchdaysProviders();

        IEnumerable<IMatchesProvider> GetAllMatchesProviders();
    }
}


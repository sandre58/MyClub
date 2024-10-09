// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface IStage : IEntity, ISchedulingParametersProvider, IMatchFormatProvider, IMatchRulesProvider, IMatchesProvider, ITeamsProvider
    {
        IEnumerable<T> GetStages<T>() where T : ICompetitionStage;

        bool RemoveMatch(Match item);
    }
}

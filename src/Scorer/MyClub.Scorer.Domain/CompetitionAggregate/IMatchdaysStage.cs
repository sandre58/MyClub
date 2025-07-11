// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface IMatchdaysStage : IStage, IMatchFormatProvider, IMatchRulesProvider, ISchedulingParametersProvider, ITeamsProvider
    {
        bool RemoveMatchday(Matchday item);
    }
}

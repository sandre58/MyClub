// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface ICompetitionStage : IStage, ISchedulingParametersProvider, ITeamsProvider, IMatchRulesProvider
    {
        string Name { get; set; }

        string ShortName { get; set; }
    }
}

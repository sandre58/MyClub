// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.ProjectAggregate
{

    public interface ISchedulingParametersRepository
    {
        SchedulingParameters GetByMatch(Match match);

        SchedulingParameters GetByMatchdaysProvider(IMatchdaysProvider matchdaysProvider);
    }
}

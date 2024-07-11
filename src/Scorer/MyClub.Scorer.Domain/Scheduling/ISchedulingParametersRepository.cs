// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.CompetitionAggregate;

namespace MyClub.Scorer.Domain.Scheduling
{

    public interface ISchedulingParametersRepository
    {
        SchedulingParameters Get(ISchedulable schedulable);

        void Update(IMatchdaysProvider matchdaysProvider, SchedulingParameters schedulingParameters);
    }
}

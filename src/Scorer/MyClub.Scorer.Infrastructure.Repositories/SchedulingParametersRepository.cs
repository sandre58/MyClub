// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class SchedulingParametersRepository(IProjectRepository projectRepository) : ISchedulingParametersRepository
    {
        private readonly IProjectRepository _projectRepository = projectRepository;

        public SchedulingParameters Get(ISchedulable schedulable)
        {
            var project = _projectRepository.GetCurrentOrThrow();

            return project.GetSchedulingParameters(schedulable);
        }

        public void Update(IMatchdaysProvider matchdaysProvider, SchedulingParameters schedulingParameters)
        {
            if (matchdaysProvider is League)
                _projectRepository.GetCurrentOrThrow().CastIn<LeagueProject>().SchedulingParameters = schedulingParameters;
            else if (matchdaysProvider is IStage stage)
                _projectRepository.GetCurrentOrThrow().CastIn<TournamentProject>().SetSchedulingParameters(stage.Id, schedulingParameters);

            _projectRepository.Save();
        }
    }
}

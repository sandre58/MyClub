// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Services;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class ParametersRepository(IProjectRepository projectRepository, IAuditService auditService) : IParametersRepository
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly IAuditService _auditService = auditService;

        public MatchFormat GetMatchFormat() => _projectRepository.GetCompetition().OrThrow().MatchFormat;

        public MatchRules GetMatchRules() => _projectRepository.GetCompetition().OrThrow().MatchRules;

        public SchedulingParameters GetSchedulingParameters() => _projectRepository.GetCompetition().OrThrow().SchedulingParameters;

        public ProjectPreferences GetPreferences() => _projectRepository.GetCurrentOrThrow().Preferences;

        public void UpdateMatchFormat(MatchFormat format)
        {
            var competition = _projectRepository.GetCompetition().OrThrow();

            competition.MatchFormat = format;

            _auditService.Update(competition);
        }

        public void UpdateMatchRules(MatchRules rules)
        {
            var competition = _projectRepository.GetCompetition().OrThrow();

            competition.MatchRules = rules;

            _auditService.Update(competition);
        }

        public void UpdateSchedulingParameters(SchedulingParameters schedulingParameters)
        {
            var competition = _projectRepository.GetCompetition().OrThrow();

            competition.SchedulingParameters = schedulingParameters;

            _auditService.Update(competition);
        }

        public void UpdatePreferences(ProjectPreferences projectPreferences)
        {
            var project = _projectRepository.GetCurrentOrThrow();

            project.Preferences.TreatNoStadiumAsWarning = projectPreferences.TreatNoStadiumAsWarning;
            project.Preferences.PeriodForNextMatches = projectPreferences.PeriodForNextMatches;
            project.Preferences.PeriodForPreviousMatches = projectPreferences.PeriodForPreviousMatches;
            project.Preferences.ShowLastMatchFallback = projectPreferences.ShowLastMatchFallback;
            project.Preferences.ShowNextMatchFallback = projectPreferences.ShowNextMatchFallback;

            _auditService.Update(project);
        }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Application.Services
{
    public class ParametersService(IParametersRepository parametersRepository, MatchService matchService)
    {
        private readonly IParametersRepository _parametersRepository = parametersRepository;
        private readonly MatchService _matchService = matchService;

        public void UpdateMatchFormat(MatchFormat matchFormat) => _parametersRepository.UpdateMatchFormat(matchFormat);

        public void UpdateMatchRules(MatchRules matchRules, bool applyMatchRulesOnExistingMatches)
        {
            _parametersRepository.UpdateMatchRules(matchRules);
            // TODO
            //if (applyMatchRulesOnExistingMatches)
            //    _matchService.UpdateRules(_matchService.GetAll().Select(x => x.Id).ToList(), matchRules);
        }

        public void UpdateSchedulingParameters(SchedulingParameters schedulingParameters) => _parametersRepository.UpdateSchedulingParameters(schedulingParameters);

        public MatchFormat GetMatchFormat() => _parametersRepository.GetMatchFormat();

        public MatchRules GetMatchRules() => _parametersRepository.GetMatchRules();

        public SchedulingParameters GetSchedulingParameters() => _parametersRepository.GetSchedulingParameters();

        public PreferencesDto GetPreferences()
        {
            var preferences = _parametersRepository.GetPreferences();

            return new()
            {
                PeriodForNextMatches = preferences.PeriodForNextMatches,
                PeriodForPreviousMatches = preferences.PeriodForPreviousMatches,
                TreatNoStadiumAsWarning = preferences.TreatNoStadiumAsWarning,
                ShowNextMatchFallback = preferences.ShowNextMatchFallback,
                ShowLastMatchFallback = preferences.ShowLastMatchFallback
            };
        }
    }
}

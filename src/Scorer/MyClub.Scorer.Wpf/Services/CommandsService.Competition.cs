// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.Services
{
    internal class CompetitionCommandsService(LeaguePresentationService leaguePresentationService,
                                              CompetitionInfoProvider competitionInfoProvider)
    {
        private readonly LeaguePresentationService _leaguePresentationService = leaguePresentationService;
        private readonly CompetitionInfoProvider _competitionInfoProvider = competitionInfoProvider;

        public async Task OpenBuildAssistantAsync()
        {
            if (_competitionInfoProvider.GetCompetition() is LeagueViewModel)
            {
                await _leaguePresentationService.OpenBuildAssistantAsync().ConfigureAwait(false);
            }
        }
    }
}

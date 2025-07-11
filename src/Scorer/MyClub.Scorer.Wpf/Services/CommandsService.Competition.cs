﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.Services.Providers;

namespace MyClub.Scorer.Wpf.Services
{
    internal class CompetitionCommandsService(LeaguePresentationService leaguePresentationService,
                                              CupPresentationService cupPresentationService,
                                              CompetitionInfoProvider competitionInfoProvider)
    {
        private readonly LeaguePresentationService _leaguePresentationService = leaguePresentationService;
        private readonly CupPresentationService _cupPresentationService = cupPresentationService;
        private readonly CompetitionInfoProvider _competitionInfoProvider = competitionInfoProvider;

        public async Task OpenBuildAssistantAsync()
        {
            if (_competitionInfoProvider.Type == CompetitionType.League)
            {
                await _leaguePresentationService.OpenBuildAssistantAsync().ConfigureAwait(false);
            }
            else if (_competitionInfoProvider.Type == CompetitionType.Cup)
            {
                await _cupPresentationService.OpenBuildAssistantAsync().ConfigureAwait(false);
            }
        }

        public async Task EditRankingRulesAsync()
        {
            if (_competitionInfoProvider.Type == CompetitionType.League)
            {
                await _leaguePresentationService.EditRankingRulesAsync().ConfigureAwait(false);
            }
        }
    }
}

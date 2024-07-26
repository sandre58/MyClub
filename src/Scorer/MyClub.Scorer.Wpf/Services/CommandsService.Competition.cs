// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;

namespace MyClub.Scorer.Wpf.Services
{
    internal class CompetitionCommandsService(IViewModelLocator viewModelLocator,
                                              LeaguePresentationService leaguePresentationService,
                                              CompetitionInfoProvider competitionInfoProvider)
    {
        private readonly LeaguePresentationService _leaguePresentationService = leaguePresentationService;
        private readonly CompetitionInfoProvider _competitionInfoProvider = competitionInfoProvider;
        private readonly IViewModelLocator _viewModelLocator = viewModelLocator;

        public async Task OpenBuildAssistantAsync()
        {
            if (_competitionInfoProvider.GetCompetition() is LeagueViewModel)
            {
                await _leaguePresentationService.OpenBuildAssistantAsync().ConfigureAwait(false);
            }
        }

        public async Task EditSchedulingParametersAsync()
        {
            var vm = _viewModelLocator.Get<SchedulingParametersEditionViewModel>();

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }
    }
}

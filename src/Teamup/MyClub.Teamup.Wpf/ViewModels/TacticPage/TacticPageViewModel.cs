// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.Navigation;
using MyNet.UI.Navigation.Models;
using MyNet.UI.ViewModels.Display;
using MyClub.Teamup.Wpf.Enums;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.TacticPage
{
    internal class TacticPageViewModel : PageViewModel
    {
        public TacticsViewModel TacticsViewModel { get; }

        public TacticPageViewModel(
            ProjectInfoProvider projectInfoProvider,
            TacticsProvider tacticsProvider,
            PlayersProvider playersProvider,
            TacticPresentationService tacticPresentationService) : base(projectInfoProvider) => TacticsViewModel = new TacticsViewModel(projectInfoProvider, tacticsProvider, playersProvider, tacticPresentationService);

        public override void LoadParameters(INavigationParameters? parameters)
        {
            if (parameters is null) return;

            if (parameters.Get<TacticViewModel>(NavigationCommandsService.ItemParameterKey) is TacticViewModel item)
                TacticsViewModel.ShowDetails(item);
        }

        protected override void Cleanup()
        {
            TacticsViewModel.Dispose();
            base.Cleanup();
        }
    }
}

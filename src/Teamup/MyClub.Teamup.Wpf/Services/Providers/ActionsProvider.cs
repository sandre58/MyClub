// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MaterialDesignThemes.Wpf;
using MyNet.UI.Toasting;
using MyNet.Utilities;
using MyNet.Observable.Collections.Providers;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class ActionsProvider : SourceProvider<ActionViewModel>
    {
        public ActionsProvider(PlayerPresentationService playerPresentationService,
                               LeaguePresentationService leaguePresentationService,
                               CupPresentationService cupPresentationService,
                               FriendlyPresentationService friendlyPresentationService,
                               TrainingSessionPresentationService trainingSessionPresentationService,
                               CompetitionsProvider competitionsProvider,
                               PlayersProvider playersProvider,
                               TrainingSessionsProvider trainingSessionsProvider) : base(new[]
        {
            new ActionViewModel(nameof(MyClubResources.AddPlayer), nameof(MyClubResources.AddPlayerDescription), async () =>
            {
                var guid = await playerPresentationService.AddAsync().ConfigureAwait(false);

                if(guid.HasValue)
                {
                    var item = playersProvider.Get(guid.Value).OrThrow();
                    ToasterManager.ShowSuccess(MyClubResources.AddPlayerSuccess, onClick: x => NavigationCommandsService.NavigateToPlayerPage(item));
                }
            }, PackIconKind.UserAdd),

            new ActionViewModel(nameof(MyClubResources.AddLeague), nameof(MyClubResources.AddLeagueDescription), async () =>
            {
                var guid = await leaguePresentationService.AddAsync().ConfigureAwait(false);

                if(guid.HasValue)
                {
                    var item = competitionsProvider.Get(guid.Value).OrThrow();
                    ToasterManager.ShowSuccess(MyClubResources.AddCompetitionSuccess, onClick: x => NavigationCommandsService.NavigateToCompetitionPage(item));
                }
            }, PackIconKind.Podium),

            new ActionViewModel(nameof(MyClubResources.AddCup), nameof(MyClubResources.AddCupDescription), async () =>
            {
                var guid = await cupPresentationService.AddAsync().ConfigureAwait(false);

                if(guid.HasValue)
                {
                    var item = competitionsProvider.Get(guid.Value).OrThrow();
                    ToasterManager.ShowSuccess(MyClubResources.AddCompetitionSuccess, onClick: x => NavigationCommandsService.NavigateToCompetitionPage(item));
                }
            }, PackIconKind.Tournament),

            new ActionViewModel(nameof(MyClubResources.AddFriendly), nameof(MyClubResources.AddFriendlyDescription), async () =>
            {
                var guid = await friendlyPresentationService.AddAsync().ConfigureAwait(false);

                if(guid.HasValue)
                {
                    var item = competitionsProvider.Get(guid.Value).OrThrow();
                    ToasterManager.ShowSuccess(MyClubResources.AddCompetitionSuccess, onClick: x => NavigationCommandsService.NavigateToCompetitionPage(item));
                }
            }, PackIconKind.Handshake),

            new ActionViewModel(nameof(MyClubResources.AddTrainingSession), nameof(MyClubResources.AddTrainingSessionDescription), async () =>
            {
                var guid = await trainingSessionPresentationService.AddAsync().ConfigureAwait(false);

                if(guid.HasValue)
                {
                    var item = trainingSessionsProvider.Get(guid.Value).OrThrow();
                    ToasterManager.ShowSuccess(MyClubResources.AddTrainingSessionSuccess, onClick: x => NavigationCommandsService.NavigateToTrainingSessionPage(item));
                }
            }, PackIconKind.Strategy)
        })
        { }
    }
}

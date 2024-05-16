// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using DynamicData;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Enums;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.CalendarPage;
using MyClub.Teamup.Wpf.ViewModels.CommunicationPage;
using MyClub.Teamup.Wpf.ViewModels.CompetitionPage;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using MyClub.Teamup.Wpf.ViewModels.HomePage;
using MyClub.Teamup.Wpf.ViewModels.MatchPage;
using MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage;
using MyClub.Teamup.Wpf.ViewModels.PlayerPage;
using MyClub.Teamup.Wpf.ViewModels.RosterPage;
using MyClub.Teamup.Wpf.ViewModels.TacticPage;
using MyClub.Teamup.Wpf.ViewModels.TrainingPage;
using MyClub.Teamup.Wpf.ViewModels.TrainingSessionPage;
using MyNet.UI.Dialogs;
using MyNet.UI.Locators;
using MyNet.UI.Navigation;
using MyNet.UI.Navigation.Models;
using MyNet.UI.ViewModels;
using MyNet.Utilities;
using MyNet.Utilities.Suspending;
using MyNet.Wpf.Schedulers;

namespace MyClub.Teamup.Wpf.Services
{
    internal sealed class NavigationCommandsService : WorkspaceNavigationService, IDisposable
    {
        public const string DisplayModeParameterKey = "DisplayMode";
        public const string DetailsTabParameterKey = "DetailsTab";
        public const string ItemParameterKey = "Item";
        public const string TabParameterKey = "Tab";
        public const string SubjectParameterKey = "Subject";
        public const string BodyParameterKey = "Body";
        public const string SendCopyParameterKey = "SendCopy";
        public const string AddressesParameterKey = "Addresses";

        private readonly CompositeDisposable _disposables = [];
        private readonly Suspender _navigationSuspender = new();

        public NavigationCommandsService(ProjectInfoProvider projectInfoProvider, PlayersProvider playersProvider, TrainingSessionsProvider trainingSessionsProvider)
        {
            _disposables.AddRange(
            [
                playersProvider.Connect().OnItemRemoved(GoToPreviousPageForDeletedItem<PlayerPageViewModel, PlayerViewModel>).Subscribe(),
                trainingSessionsProvider.Connect().OnItemRemoved(GoToPreviousPageForDeletedItem<TrainingSessionPageViewModel, TrainingSessionViewModel>).Subscribe(),
            ]);

            IDisposable? navigationSuspenderScope = null;
            projectInfoProvider.WhenProjectChanging(_ => navigationSuspenderScope = _navigationSuspender.Suspend());
            projectInfoProvider.WhenProjectChanged(x =>
            {
                if (CurrentContext?.Page is not HomePageViewModel)
                    WpfScheduler.Current.Schedule(() => NavigateToHomePage());
                ClearJournal();

                navigationSuspenderScope?.Dispose();
            });
        }

        private void GoToPreviousPageForDeletedItem<TPage, T>(T item) where TPage : IItemViewModel<T>
        {
            if (_navigationSuspender.IsSuspended) return;

            RemoveItemEntries(item);

            if (CurrentContext?.Page is TPage pageViewModel && Equals(pageViewModel.Item, item))
                WpfScheduler.Current.Schedule(() =>
                {
                    using (JournalSuspender.Suspend())
                        GoBack();
                });
        }

        private void RemoveItemEntries<T>(T item)
        {
            var backEntriesToDelete = GetBackJournal().Where(y => y.Parameters is not null && y.Parameters.Get<T>(ItemParameterKey) is T parameter && Equals(parameter, item)).ToList();
            backEntriesToDelete.ForEach(RemoveBackEntry);

            var forwardEntriesToDelete = GetForwardJournal().Where(y => y.Parameters is not null && y.Parameters.Get<T>(ItemParameterKey) is T parameter && Equals(parameter, item)).ToList();
            forwardEntriesToDelete.ForEach(RemoveForwardEntry);
        }

        public void Dispose() => _disposables.Dispose();

        public static void NavigateToHomePage() => NavigationManager.NavigateTo<HomePageViewModel>();

        public static void NavigateToCalendarPage() => NavigationManager.NavigateTo<CalendarPageViewModel>();

        public static async Task NavigateToCommunicationPageAsync(IEnumerable<string>? addresses = null, string? subject = null, string? body = null, bool? sendACopy = false)
        {
            var parameters = new List<KeyValuePair<string, object?>>();

            if (addresses is not null) parameters.Add(new KeyValuePair<string, object?>(AddressesParameterKey, addresses));
            if (subject is not null) parameters.Add(new KeyValuePair<string, object?>(SubjectParameterKey, subject));
            if (body is not null) parameters.Add(new KeyValuePair<string, object?>(BodyParameterKey, body));
            if (sendACopy is not null) parameters.Add(new KeyValuePair<string, object?>(SendCopyParameterKey, sendACopy));

            if (ViewModelManager.Get<CommunicationPageViewModel>().EmailViewModel.IsModified())
            {
                var question = await DialogManager.ShowQuestionWithCancelAsync(MyClubResources.EmailIsModifiedQuestion).ConfigureAwait(false);

                switch (question)
                {
                    case MessageBoxResult.No:
                        parameters.Clear();
                        if (addresses is not null) parameters.Add(new KeyValuePair<string, object?>(AddressesParameterKey, addresses));
                        break;
                    case MessageBoxResult.Yes:
                        break;
                    default:
                        return;
                }
            }

            MyNet.Observable.Threading.Scheduler.GetUIOrCurrent().Schedule(() => NavigationManager.NavigateTo<CommunicationPageViewModel>(parameters));
        }

        public static bool NavigateToInjuryPage(InjuryViewModel item) => NavigateToItem<PlayerPageViewModel>(item);

        public static bool NavigateToMedicalCenterPage(MedicalCenterPageTab? tab = null) => NavigateTo<MedicalCenterPageViewModel>(tab);

        public static bool NavigateToPlayerPage(PlayerViewModel item, PlayerPageTab? tab = null) => NavigateToItem<PlayerPageViewModel>(item, tab);

        public static bool NavigateToRosterPage() => NavigateTo<RosterPageViewModel>();

        public static bool NavigateToTrainingSessionPage(TrainingSessionViewModel item, TrainingSessionPageTab? tab = null) => NavigateToItem<TrainingSessionPageViewModel>(item, tab);

        public static bool NavigateToTrainingPage(TrainingPageTab? tab = null) => NavigateTo<TrainingPageViewModel>(tab);

        public static bool NavigateToTrainingCalendarPage()
        {
            var parameters = new List<KeyValuePair<string, object?>>
            {
                new(DisplayModeParameterKey, TrainingSessionsDisplayMode.Calendar)
            };

            return NavigationManager.NavigateTo<TrainingPageViewModel>(parameters);
        }

        public static void NavigateToTacticPage(TacticViewModel item) => NavigateToItem<TacticPageViewModel>(item);

        public static void NavigateToCompetitionPage(CompetitionViewModel item)
        {
            switch (item)
            {
                case LeagueViewModel league:
                    NavigateToItem<LeaguePageViewModel>(league);
                    break;
                case CupViewModel cup:
                    NavigateToItem<CupPageViewModel>(cup);
                    break;
                case FriendlyViewModel friendly:
                    NavigateToItem<FriendlyPageViewModel>(friendly);
                    break;
                default:
                    break;
            }
        }

        public static void NavigateToMatchday(MatchdayViewModel item)
        {
            switch (item.Parent)
            {
                case LeagueViewModel _:
                    NavigateToItem<LeaguePageViewModel>(item);
                    break;
                case GroupStageViewModel groupStage:
                    NavigateToItem<CupPageViewModel>(groupStage);
                    break;
                default:
                    break;
            }
        }

        public static void NavigateToRound(RoundViewModel item) => NavigateToItem<CupPageViewModel>(item);

        public static void NavigateToMatchPage(MatchViewModel item) => NavigateToItem<MatchPageViewModel>(item);

        internal static void NavigateToMatchParent(IMatchParent parent)
        {
            switch (parent)
            {
                case MatchdayViewModel matchday:
                    NavigateToMatchday(matchday);
                    break;

                case RoundViewModel round:
                    NavigateToRound(round);
                    break;

                case CompetitionViewModel competition:
                    NavigateToCompetitionPage(competition);
                    break;
            }
        }

        private static bool NavigateTo<T>(object? tab = null) where T : INavigationPage
        {
            var parameters = new List<KeyValuePair<string, object?>>();

            if (tab is not null) parameters.Add(new KeyValuePair<string, object?>(TabParameterKey, tab));

            return NavigationManager.NavigateTo<T>(parameters);
        }

        private static bool NavigateToItem<T>(object item, object? tab = null) where T : INavigationPage
        {
            var parameters = new List<KeyValuePair<string, object?>>
            {
                new(ItemParameterKey, item)
            };

            if (tab is not null) parameters.Add(new KeyValuePair<string, object?>(TabParameterKey, tab));

            return NavigationManager.NavigateTo<T>(parameters);
        }
    }
}

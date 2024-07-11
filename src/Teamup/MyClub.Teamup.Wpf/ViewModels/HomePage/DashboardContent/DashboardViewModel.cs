// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Messaging;
using MyNet.Observable;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Enums;
using MyClub.Teamup.Wpf.Messages;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.OverviewTab;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.HomePage.DashboardContent
{
    internal class DashboardViewModel : ObservableObject
    {
        private readonly MainItemsProvider _mainItemsProvider;
        private readonly ReadOnlyObservableCollection<TeamWrapper> _teams;

        [DoNotCheckEquality]
        public Project? CurrentProject { get; set; }

        public OverviewAbsencesViewModel AbsencesViewModel { get; set; }

        public OverviewInjuriesViewModel MedicalCenterViewModel { get; set; }

        public OverviewCalendarViewModel CalendarViewModel { get; set; }

        public ReadOnlyObservableCollection<TeamWrapper> Teams => _teams;

        public OverviewTrainingSessionViewModel LastTrainingSessionViewModel { get; private set; }

        public OverviewTrainingSessionViewModel NextTrainingSessionViewModel { get; private set; }

        public ICommand NavigateToMedicalCenterCommand { get; }

        public ICommand NavigateToRosterCommand { get; }

        public ICommand AddPlayerCommand { get; }

        public ICommand AddTrainingSessionCommand { get; }

        public DashboardViewModel(TrainingSessionPresentationService trainingSessionPresentationService,
                                  PlayerPresentationService playerPresentationService,
                                  ProjectInfoProvider projectInfoProvider,
                                  MainItemsProvider mainItemsProvider,
                                  TeamsProvider teamsProvider,
                                  PlayersProvider playersProvider,
                                  TrainingSessionsProvider trainingSessionsProvider,
                                  HolidaysProvider holidaysProvider,
                                  InjuriesStatisticsRefreshDeferrer injuriesStatisticsRefreshDeferrer,
                                  TrainingStatisticsRefreshDeferrer trainingStatisticsRefreshDeferrer)
        {
            _mainItemsProvider = mainItemsProvider;

            MedicalCenterViewModel = new();
            AbsencesViewModel = new();
            CalendarViewModel = new(mainItemsProvider, holidaysProvider, trainingSessionPresentationService);
            LastTrainingSessionViewModel = new(2);
            NextTrainingSessionViewModel = new(2);

            NavigateToMedicalCenterCommand = CommandsManager.Create(() => NavigationCommandsService.NavigateToMedicalCenterPage(MedicalCenterPageTab.Overview));
            NavigateToRosterCommand = CommandsManager.Create(() => NavigationCommandsService.NavigateToRosterPage());
            AddPlayerCommand = CommandsManager.CreateNotNull<Guid>(async x => await playerPresentationService.AddAsync(x).ConfigureAwait(false));
            AddTrainingSessionCommand = CommandsManager.CreateNotNull<Guid>(async x => await trainingSessionPresentationService.AddAsync(null, teamId: x).ConfigureAwait(false));

            projectInfoProvider.WhenProjectChanged(x => CurrentProject = x);

            Disposables.AddRange(
            [
                teamsProvider.ConnectMyTeams().AutoRefresh(x => x.Order).Transform(x => new TeamWrapper(x, playersProvider, trainingSessionsProvider)).Sort(SortExpressionComparer<TeamWrapper>.Ascending(x => x.Item.Order)).ObserveOn(MyNet.UI.Threading.Scheduler.UI).Bind(out _teams).DisposeMany().Subscribe()
            ]);

            injuriesStatisticsRefreshDeferrer.Subscribe(this, RefreshPlayers);
            trainingStatisticsRefreshDeferrer.Subscribe(this, RefreshTrainings);

            Messenger.Default.Register<MainTeamChangedMessage>(this, _ =>
            {
                RefreshPlayers();
                RefreshTrainings();
            });
        }

        private void RefreshPlayers()
        {
            try
            {
                LogManager.Trace($"Refresh - Overview [Players]");

                var currentInjuries = _mainItemsProvider.Players.SelectMany(x => x.Injuries.Where(y => y.IsCurrent)).ToList();
                var currentAbsences = _mainItemsProvider.Players.SelectMany(x => x.Absences.Where(y => y.IsCurrent)).ToList();

                MedicalCenterViewModel.Refresh(currentInjuries);
                AbsencesViewModel.Refresh(currentAbsences);
            }
            catch (TaskCanceledException)
            {
                LogManager.Debug("Computing players statistics has been cancelled.");
            }
        }

        private void RefreshTrainings()
        {
            try
            {
                LogManager.Trace($"Refresh - Overview [Trainings]");

                var sessions = _mainItemsProvider.TrainingSessions.OrderBy(x => x.StartDate).ToList();

                LastTrainingSessionViewModel.Refresh(sessions.LastOrDefault(x => x.EndDate < DateTime.Now));
                NextTrainingSessionViewModel.Refresh(sessions.Where(x => !x.IsCancelled).FirstOrDefault(x => x.StartDate > DateTime.Now));
            }
            catch (TaskCanceledException)
            {
                LogManager.Debug("Computing trainings statistics has been cancelled.");
            }
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            MedicalCenterViewModel.Dispose();
            CalendarViewModel.Dispose();
            AbsencesViewModel.Dispose();
            LastTrainingSessionViewModel.Dispose();
            NextTrainingSessionViewModel.Dispose();
        }
    }

    internal class TeamWrapper : Wrapper<TeamViewModel>
    {
        public int CountPlayers { get; private set; }

        public int CountTrainingSessions { get; private set; }

        public TeamWrapper(TeamViewModel item,
                            PlayersProvider playersProvider,
                            TrainingSessionsProvider trainingsProvider) : base(item)
            => Disposables.AddRange(
            [
                playersProvider.Connect().AutoRefresh(x => x.TeamId).Filter(x => x.TeamId == item.Id).Count().Subscribe(x => CountPlayers = x),
                trainingsProvider.Connect().AutoRefresh(x => x.Teams).Filter(x => x.Teams.Contains(item)).Count().Subscribe(x => CountTrainingSessions = x)
            ]);
    }
}

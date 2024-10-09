// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Binding;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Deferrers;
using MyNet.Utilities;
using MyNet.Utilities.Logging;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    public enum CompetitionState
    {
        Incoming,

        InProgress,

        Finished
    }

    internal class CompetitionInfoProvider : ObservableObject
    {
        private readonly ProjectInfoProvider _projectInfoProvider;
        private readonly LeagueService _leagueService;
        private CompositeDisposable _competitionDisposables = [];
        private ICompetitionViewModel? _currentCompetition;

        public CompetitionInfoProvider(ProjectInfoProvider projectInfoProvider,
                                       MatchdayPresentationService matchdayPresentationService,
                                       RoundPresentationService roundPresentationService,
                                       MatchPresentationService matchPresentationService,
                                       StadiumsProvider stadiumsProvider,
                                       TeamsProvider teamsProvider,
                                       LeagueService leagueService)
        {
            _projectInfoProvider = projectInfoProvider;
            _leagueService = leagueService;

            UnloadRunner = new(() =>
            {
                StartDate = null;
                EndDate = null;
                Type = null;
                State = null;
                _competitionDisposables.Dispose();
                _currentCompetition?.Dispose();
                _currentCompetition = null;
            });
            LoadRunner = new(x =>
            {
                Type = x.Type;
                _currentCompetition = x switch
                {
                    LeagueProject leagueProject => new LeagueViewModel(leagueProject.Competition,
                                                                       leagueProject.Competition.WhenChanged(x => x.SchedulingParameters, (x, y) => y),
                                                                       matchdayPresentationService,
                                                                       matchPresentationService,
                                                                       stadiumsProvider,
                                                                       teamsProvider,
                                                                       _leagueService),
                    CupProject cupProject => new CupViewModel(cupProject.Competition,
                                                              cupProject.Competition.WhenChanged(x => x.SchedulingParameters, (x, y) => y),
                                                              roundPresentationService,
                                                              matchPresentationService,
                                                              stadiumsProvider,
                                                              teamsProvider),
                    TournamentProject => _currentCompetition = null,
                    _ => null
                };

                if (_currentCompetition is null) return;

                _competitionDisposables = new(x.WhenPropertyChanged(x => x.Competition.SchedulingParameters).Subscribe(y =>
                {
                    StartDate = y.Value?.StartDate;
                    EndDate = y.Value?.EndDate;

                    if (StartDate.HasValue && EndDate.HasValue)
                        State = StartDate.Value.IsInFuture() ? CompetitionState.Incoming : EndDate.Value.IsInPast() ? CompetitionState.Finished : CompetitionState.InProgress;
                }));
            }, true);
            LoadRunner.RegisterOnEnd(this, x => LogManager.Trace($"{GetType().Name} : Load {x?.GetType()} in {LoadRunner.LastTimeElapsed.Milliseconds}ms"));

            projectInfoProvider.UnloadRunner.RegisterOnStart(this, Unload);
            projectInfoProvider.LoadRunner.RegisterOnEnd(this, Reload);
        }

        public CompetitionType? Type { get; private set; }

        public DateOnly? StartDate { get; private set; }

        public DateOnly? EndDate { get; private set; }

        public CompetitionState? State { get; private set; }

        public ActionRunner UnloadRunner { get; }

        public ActionRunner<IProject, ICompetitionViewModel> LoadRunner { get; }

        public ICompetitionViewModel GetCompetition() => _currentCompetition!;

        public LeagueViewModel GetLeague() => (LeagueViewModel)_currentCompetition! ?? throw new InvalidCastException($"Competition is not of type {typeof(LeagueViewModel)}");

        public CupViewModel GetCup() => (CupViewModel)_currentCompetition! ?? throw new InvalidCastException($"Competition is not of type {typeof(CupViewModel)}");

        private void Unload()
        {
            if (_currentCompetition is null) return;

            UnloadRunner.Run();
        }

        protected void Reload(IProject project) => LoadRunner.Run(project, () => _currentCompetition!);

        protected override void Cleanup()
        {
            base.Cleanup();
            _projectInfoProvider.LoadRunner.Unregister(this);
            _projectInfoProvider.UnloadRunner.Unregister(this);
            LoadRunner.Dispose();
            UnloadRunner.Dispose();
            _currentCompetition?.Dispose();
        }
    }
}

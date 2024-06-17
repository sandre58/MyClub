﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class CompetitionInfoProvider : ObservableObject
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly StadiumsProvider _stadiumsProvider;
        private readonly TeamsProvider _teamsProvider;
        private readonly LeagueService _leagueService;
        private CompositeDisposable? _disposables;
        private ICompetitionViewModel? _currentCompetition;

        public CompetitionInfoProvider(ProjectInfoProvider projectInfoProvider,
                                       MatchdayPresentationService matchdayPresentationService,
                                       MatchPresentationService matchPresentationService,
                                       StadiumsProvider stadiumsProvider,
                                       TeamsProvider teamsProvider,
                                       LeagueService leagueService)
        {
            _matchdayPresentationService = matchdayPresentationService;
            _matchPresentationService = matchPresentationService;
            _stadiumsProvider = stadiumsProvider;
            _teamsProvider = teamsProvider;
            _leagueService = leagueService;

            projectInfoProvider.WhenProjectClosing(Clear);
            projectInfoProvider.WhenProjectLoaded(Reload);
        }

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public TimeSpan DefaultTime { get; private set; }

        public TimeSpan RotationTime { get; private set; }

        public TimeSpan MinimumRestTime { get; private set; }

        public ICompetitionViewModel GetCompetition() => GetCompetition<ICompetitionViewModel>();

        public T GetCompetition<T>() where T : ICompetitionViewModel => (T)_currentCompetition! ?? throw new InvalidCastException($"Competition is not of type {typeof(T)}");

        private void Clear()
        {
            _disposables?.Dispose();
            _currentCompetition?.Dispose();
            DefaultTime = TimeSpan.Zero;
            MinimumRestTime = TimeSpan.Zero;
            RotationTime = TimeSpan.Zero;
        }

        protected void Reload(IProject project)
        {
            _currentCompetition = project.Competition switch
            {
                League league => new LeagueViewModel(league, _matchdayPresentationService, _matchPresentationService, _stadiumsProvider, _teamsProvider, _leagueService),
                Cup cup => new CupViewModel(cup),
                _ => null,
            };
            StartDate = project.StartDate;
            EndDate = project.EndDate;
            _disposables = new(project.Parameters.WhenPropertyChanged(x => x.MatchStartTime).Subscribe(x => DefaultTime = x.Value),
                project.Parameters.WhenPropertyChanged(x => x.RotationTime).Subscribe(x => RotationTime = x.Value),
                project.Parameters.WhenPropertyChanged(x => x.MinimumRestTime).Subscribe(x => MinimumRestTime = x.Value));
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            _disposables?.Dispose();
            _currentCompetition?.Dispose();
        }
    }
}

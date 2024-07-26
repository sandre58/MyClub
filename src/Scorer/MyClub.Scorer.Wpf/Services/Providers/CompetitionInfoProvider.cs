// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Wpf.Messages;
using MyClub.Scorer.Wpf.Services.Deferrers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Deferrers;
using MyNet.UI.Services;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Messaging;
using MyNet.Utilities.Threading;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class CompetitionInfoProvider : ObservableObject
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly AvailibilityCheckingService _availibilityCheckingService;
        private readonly StadiumsProvider _stadiumsProvider;
        private readonly TeamsProvider _teamsProvider;
        private readonly LeagueService _leagueService;
        private readonly ResultsChangedDeferrer _resultsChangedDeferrer;
        private readonly TeamsChangedDeferrer _teamsChangedDeferrer;
        private readonly ScheduleChangedDeferrer _scheduleChangedDeferrer;
        private readonly StadiumsChangedDeferrer _stadiumsChangedDeferrer;
        private readonly SingleTaskRunner _checkConflictsRunner;
        private readonly RefreshDeferrer _checkConflictsDeferrer = new();
        private readonly Subject<ICompetitionViewModel> _competitionLoadedSubject = new();
        private readonly Subject<ICompetitionViewModel> _competitionDisposingSubject = new();
        private ICompetitionViewModel? _currentCompetition;

        public CompetitionInfoProvider(ProjectInfoProvider projectInfoProvider,
                                       MatchdayPresentationService matchdayPresentationService,
                                       MatchPresentationService matchPresentationService,
                                       StadiumsProvider stadiumsProvider,
                                       TeamsProvider teamsProvider,
                                       LeagueService leagueService,
                                       AvailibilityCheckingService availibilityCheckingService,
                                       ResultsChangedDeferrer resultsChangedDeferrer,
                                       TeamsChangedDeferrer teamsChangedDeferrer,
                                       ScheduleChangedDeferrer scheduleChangedDeferrer,
                                       StadiumsChangedDeferrer stadiumsChangedDeferrer)
        {
            _matchdayPresentationService = matchdayPresentationService;
            _matchPresentationService = matchPresentationService;
            _stadiumsProvider = stadiumsProvider;
            _teamsProvider = teamsProvider;
            _availibilityCheckingService = availibilityCheckingService;
            _leagueService = leagueService;
            _resultsChangedDeferrer = resultsChangedDeferrer;
            _teamsChangedDeferrer = teamsChangedDeferrer;
            _scheduleChangedDeferrer = scheduleChangedDeferrer;
            _stadiumsChangedDeferrer = stadiumsChangedDeferrer;
            _checkConflictsRunner = new SingleTaskRunner(async x => await CheckConflictsAsync(x).ConfigureAwait(false));

            _checkConflictsDeferrer.Subscribe(this, _checkConflictsRunner.Run, 80);

            projectInfoProvider.WhenProjectClosing(Clear);
            projectInfoProvider.WhenProjectLoaded(Reload);
        }

        public bool HasMatches { get; private set; }

        public ICompetitionViewModel GetCompetition() => GetCompetition<ICompetitionViewModel>();

        public T GetCompetition<T>() where T : ICompetitionViewModel => (T)_currentCompetition! ?? throw new InvalidCastException($"Competition is not of type {typeof(T)}");

        private void Clear()
        {
            if (_currentCompetition is null) return;

            _competitionDisposingSubject.OnNext(_currentCompetition);

            _stadiumsChangedDeferrer.Unsubscribe(this);
            _scheduleChangedDeferrer.Unsubscribe(this);
            _teamsChangedDeferrer.Unsubscribe(this);
            _currentCompetition.Dispose();

            _currentCompetition = null;
        }

        protected void Reload(IProject project)
        {
            switch (project)
            {
                case LeagueProject leagueProject:
                    var league = new LeagueViewModel(leagueProject.Competition,
                                                     leagueProject.Competition.WhenChanged(x => x.SchedulingParameters, (x, y) => y),
                                                     _matchdayPresentationService,
                                                     _matchPresentationService,
                                                     _stadiumsProvider,
                                                     _teamsProvider,
                                                     _leagueService,
                                                     _resultsChangedDeferrer,
                                                     _teamsChangedDeferrer);

                    _currentCompetition = league;
                    break;
                case CupProject cupProject:
                    _currentCompetition = new CupViewModel(cupProject.Competition);
                    break;
                case TournamentProject _:
                    _currentCompetition = null;
                    break;
                default:
                    break;
            }

            if (_currentCompetition is null) return;

            _scheduleChangedDeferrer.Subscribe(this, _checkConflictsDeferrer.AskRefresh);
            _teamsChangedDeferrer.Subscribe(this, _checkConflictsDeferrer.AskRefresh);
            _stadiumsChangedDeferrer.Subscribe(this, _checkConflictsDeferrer.AskRefresh);

            _competitionLoadedSubject.OnNext(_currentCompetition);

            _resultsChangedDeferrer.AskRefresh();
            _teamsChangedDeferrer.AskRefresh();
            _scheduleChangedDeferrer.AskRefresh();
            _stadiumsChangedDeferrer.AskRefresh();
        }

        private async Task CheckConflictsAsync(CancellationToken cancellationToken)
        {
            if (GetCompetition() is not ICompetitionViewModel competition) return;

            LogManager.Debug("Check conflicts");

            await AppBusyManager.BackgroundAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (competition.ProvideMatches().Bind(out var matches).Subscribe())
                {
                    HasMatches = matches.Count > 0;

                    var conflicts = _availibilityCheckingService.GetAllConflicts();
                    var convertedConflicts = conflicts.Select(x => (x.Item1, matches.GetById(x.Item2), x.Item3.HasValue ? matches.GetById(x.Item3.Value) : null)).ToList();

                    foreach (var match in matches)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var matchConflicts = convertedConflicts.Where(x => match.Id == x.Item2.Id).Select(x => new MatchConflict(x.Item1, x.Item3))
                            .Union(convertedConflicts.Where(x => match.Id == x.Item3?.Id).Select(x => new MatchConflict(x.Item1, x.Item2))).ToList();

                        match.SetConflicts(matchConflicts);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    Messenger.Default.Send(new MatchConflictsValidationMessage(convertedConflicts));
                }
            }).ConfigureAwait(false);
        }

        public void WhenCompetitionChanged(Action<ICompetitionViewModel>? onLoaded = null, Action<ICompetitionViewModel>? onDisposing = null)
        {
            if (onLoaded is not null)
                Disposables.Add(_competitionLoadedSubject.Subscribe(onLoaded));

            if (onDisposing is not null)
                Disposables.Add(_competitionDisposingSubject.Subscribe(onDisposing));
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            _stadiumsChangedDeferrer.Unsubscribe(this);
            _scheduleChangedDeferrer.Unsubscribe(this);
            _teamsChangedDeferrer.Unsubscribe(this);
            _currentCompetition?.Dispose();
            _checkConflictsRunner.Dispose();
            _checkConflictsDeferrer.Dispose();
            _competitionDisposingSubject.Dispose();
            _competitionLoadedSubject.Dispose();
        }
    }
}

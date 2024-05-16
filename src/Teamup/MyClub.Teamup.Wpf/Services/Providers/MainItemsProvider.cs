// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using MyNet.Utilities;
using MyNet.Utilities.Messaging;
using MyNet.Observable;
using MyClub.Teamup.Wpf.Messages;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal sealed class MainItemsProvider : IDisposable
    {
        private readonly ObservableCollectionExtended<TrainingSessionViewModel> _trainingSessions = [];
        private readonly ObservableCollectionExtended<PlayerViewModel> _players = [];
        private readonly ObservableCollectionExtended<CompetitionViewModel> _competitions = [];
        private readonly ObservableCollectionExtended<MatchViewModel> _matches = [];
        private readonly CompositeDisposable _disposables = [];
        private readonly IObservable<IChangeSet<TrainingSessionViewModel>> _observableTrainings;
        private readonly IObservable<IChangeSet<PlayerViewModel>> _observablePlayers;
        private readonly IObservable<IChangeSet<CompetitionViewModel>> _observableCompetitions;
        private readonly IObservable<IChangeSet<MatchViewModel>> _observableMatches;
        private readonly Subject<List<Guid>> _mainTeamsFilterSubject = new();

        public MainItemsProvider(PlayersProvider playersProvider,
                              TrainingSessionsProvider trainingSessionsProvider,
                              CompetitionsProvider competitionsProvider)
        {
            TrainingSessions = new(_trainingSessions);
            Players = new(_players);
            Competitions = new(_competitions);
            Matches = new(_matches);
            _observableTrainings = _trainingSessions.ToObservableChangeSet();
            _observablePlayers = _players.ToObservableChangeSet();
            _observableCompetitions = _competitions.ToObservableChangeSet();
            _observableMatches = _matches.ToObservableChangeSet();

            var trainingsDynamicFilter = _mainTeamsFilterSubject.Select(x => new Func<TrainingSessionViewModel, bool>(y => y.Teams.Any(z => x.Contains(z.Id))));
            var playersDynamicFilter = _mainTeamsFilterSubject.Select(x => new Func<PlayerViewModel, bool>(y => y.TeamId is not null && x.Contains(y.TeamId)));
            var competitionsDynamicFilter = _mainTeamsFilterSubject.Select(x => new Func<CompetitionViewModel, bool>(y => y.Teams.Any(z => x.Contains(z.Id))));
            var matchesDynamicFilter = _mainTeamsFilterSubject.Select(x => new Func<MatchViewModel, bool>(y => x.Contains(y.HomeTeam.Id) || x.Contains(y.AwayTeam.Id)));

            _disposables.AddRange(
            [
                playersProvider.Connect().AutoRefreshOnObservable(x => x.WhenPropertyChanged(y => y.TeamId)).Filter(playersDynamicFilter).Bind(_players).Subscribe(),
                trainingSessionsProvider.Connect().AutoRefreshOnObservable(x => x.WhenPropertyChanged(y => y.Teams)).Filter(trainingsDynamicFilter).Bind(_trainingSessions).Subscribe(),
                competitionsProvider.Connect().AutoRefreshOnObservable(x => x.WhenPropertyChanged(y => y.Teams)).Filter(competitionsDynamicFilter).Bind(_competitions).Subscribe(),
                competitionsProvider.Connect().MergeMany(x => x.Matches.ToObservableChangeSet(x => x.Id)).Filter(matchesDynamicFilter).Bind(_matches).Subscribe()
            ]);

            Messenger.Default.Register<MainTeamChangedMessage>(this, OnMainTeamChanged);
        }

        private void OnMainTeamChanged(MainTeamChangedMessage message) => _mainTeamsFilterSubject.OnNext(message.MainTeams.Select(x => x.Id).ToList());

        public ReadOnlyObservableCollection<PlayerViewModel> Players { get; }

        public ReadOnlyObservableCollection<TrainingSessionViewModel> TrainingSessions { get; }

        public ReadOnlyObservableCollection<CompetitionViewModel> Competitions { get; }

        public ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        public IObservable<IChangeSet<PlayerViewModel>> ConnectPlayers() => _observablePlayers;

        public IObservable<IChangeSet<TrainingSessionViewModel>> ConnectTrainingSessions() => _observableTrainings;

        public IObservable<IChangeSet<CompetitionViewModel>> ConnectCompetitions() => _observableCompetitions;

        public IObservable<IChangeSet<MatchViewModel>> ConnectMatches() => _observableMatches;

        public IObservable<IChangeSet<IAppointment>> ConnectEvents() => ConnectTrainingSessions().Transform(x => (IAppointment)x).Merge(ConnectMatches().Transform(x => (IAppointment)x));

        public void Dispose()
        {
            _disposables.Dispose();
            Messenger.Default.Unregister(this);
        }
    }
}

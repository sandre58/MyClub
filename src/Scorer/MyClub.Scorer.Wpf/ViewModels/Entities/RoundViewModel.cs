// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.UI.Threading;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal abstract class RoundViewModel<T> : EntityViewModelBase<T>, IRoundViewModel
        where T : IRound
    {
        private readonly RoundPresentationService _roundPresentationService;
        private readonly ReadOnlyObservableCollection<IVirtualTeamViewModel> _teams;
        private readonly UiObservableCollection<MatchViewModel> _matches = [];

        protected RoundViewModel(T item,
                                 CupViewModel stage,
                                 IObservable<SchedulingParameters?> observableSchedulingParameters,
                                 RoundPresentationService roundPresentationService,
                                 MatchPresentationService matchPresentationService,
                                 TeamsProvider teamsProvider,
                                 StadiumsProvider stadiumsProvider) : base(item)
        {
            _roundPresentationService = roundPresentationService;
            Stage = stage;
            Matches = new(_matches);
            SchedulingParameters = new SchedulingParametersViewModel(observableSchedulingParameters);

            OpenCommand = CommandsManager.Create(async () => await OpenAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                ConnectMatches(matchPresentationService, stadiumsProvider, teamsProvider).ObserveOn(Scheduler.UI).Bind(_matches).Subscribe(),
                item.Teams.ToObservableChangeSet()
                          .Transform(teamsProvider.GetVirtualTeam)
                          .Bind(out _teams)
                          .Subscribe(),
                _matches.ToObservableChangeSet()
                        .WhenPropertyChanged(x => x.Date)
                        .Subscribe(_ =>
                        {
                            Date = Matches.MinOrDefault(m => m.Date);
                            MatchTime = Date.ToTime();
                            StartDate = Matches.MinOrDefault(m => m.Date.BeginningOfDay());
                            EndDate = Matches.MaxOrDefault(m => m.Date.EndOfDay());
                        }),
                _matches.ToObservableChangeSet()
                        .WhenPropertyChanged(x => x.State)
                        .Subscribe(_ => IsPostponed = Matches.All(x => x.IsPostponed)),
            ]);
        }

        public CupViewModel Stage { get; }

        public SchedulingParametersViewModel SchedulingParameters { get; }

        public string Name => Item.Name;

        public string ShortName => Item.ShortName;

        public DateTime Date { get; private set; }

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public TimeOnly MatchTime { get; set; }

        public bool IsPostponed { get; set; }

        public ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        public ReadOnlyObservableCollection<IVirtualTeamViewModel> Teams => _teams;

        public ICommand OpenCommand { get; }

        protected abstract IObservable<IChangeSet<MatchViewModel>> ConnectMatches(MatchPresentationService matchPresentationService, StadiumsProvider stadiumsProvider, TeamsProvider teamsProvider);

        public bool CanAutomaticReschedule() => Item.ProvideSchedulingParameters().CanAutomaticReschedule();

        public bool CanAutomaticRescheduleVenue() => Item.ProvideSchedulingParameters().CanAutomaticRescheduleVenue();

        public bool CanBePostponed() => !IsPostponed;

        public bool CanCancelMatch() => true;

        public bool CanEditMatchFormat() => true;

        public bool CanEditMatchRules() => true;

        public IEnumerable<IVirtualTeamViewModel> GetAvailableTeams() => Teams;

        public async Task OpenAsync() => await _roundPresentationService.OpenAsync(this).ConfigureAwait(false);
    }
}

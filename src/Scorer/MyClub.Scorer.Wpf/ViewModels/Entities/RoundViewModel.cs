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
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.UI.Commands;
using MyNet.UI.Threading;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal abstract class RoundViewModel<T> : EntityViewModelBase<T>, IRoundViewModel where T : IRound
    {
        private readonly RoundPresentationService _roundPresentationService;
        private readonly ExtendedObservableCollection<IVirtualTeamViewModel> _teams = [];
        private readonly ExtendedObservableCollection<MatchViewModel> _matches = [];

        protected RoundViewModel(T item,
                                 IRoundsStageViewModel stage,
                                 IObservable<SchedulingParameters?> observableSchedulingParameters,
                                 RoundPresentationService roundPresentationService,
                                 MatchPresentationService matchPresentationService,
                                 TeamsProvider teamsProvider,
                                 StadiumsProvider stadiumsProvider) : base(item)
        {
            _roundPresentationService = roundPresentationService;
            Stage = stage;
            Matches = new(_matches);
            Teams = new(_teams);
            SchedulingParameters = new SchedulingParametersViewModel(observableSchedulingParameters);

            OpenCommand = CommandsManager.Create(async () => await OpenAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                ConnectMatches(matchPresentationService, stadiumsProvider, teamsProvider).ObserveOn(Scheduler.GetUIOrCurrent()).Bind(_matches).Subscribe(),
                item.Teams.ToObservableChangeSet()
                          .Transform(teamsProvider.GetVirtualTeam)
                          .ObserveOn(Scheduler.GetUIOrCurrent())
                          .Bind(_teams)
                          .Subscribe(),
                _matches.ToObservableChangeSet()
                        .WhenPropertyChanged(x => x.Date)
                        .Subscribe(_ =>
                        {
                            Date = Matches.MinOrDefault(m => m.Date);
                            StartDate = Matches.MinOrDefault(m => m.StartDate);
                            EndDate = Matches.MaxOrDefault(m => m.EndDate);
                        }),
                _matches.ToObservableChangeSet()
                        .WhenPropertyChanged(x => x.State)
                        .Subscribe(_ => IsPostponed = Matches.All(x => x.IsPostponed)),
            ]);
        }

        public IRoundsStageViewModel Stage { get; }

        public SchedulingParametersViewModel SchedulingParameters { get; }

        public string Name => Item.Name;

        public string ShortName => Item.ShortName;

        public DateTime Date { get; private set; }

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public bool IsPostponed { get; set; }

        public ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        public ReadOnlyObservableCollection<IVirtualTeamViewModel> Teams { get; }

        public ICommand OpenCommand { get; }

        protected abstract IObservable<IChangeSet<MatchViewModel>> ConnectMatches(MatchPresentationService matchPresentationService, StadiumsProvider stadiumsProvider, TeamsProvider teamsProvider);

        public bool CanAutomaticReschedule() => Item.ProvideSchedulingParameters().CanAutomaticReschedule();

        public bool CanAutomaticRescheduleVenue() => Item.ProvideSchedulingParameters().CanAutomaticRescheduleVenue();

        public bool CanBePostponed() => !IsPostponed;

        public IEnumerable<IVirtualTeamViewModel> GetAvailableTeams() => Teams;

        public async Task OpenAsync() => await _roundPresentationService.OpenAsync(this).ConfigureAwait(false);
    }
}

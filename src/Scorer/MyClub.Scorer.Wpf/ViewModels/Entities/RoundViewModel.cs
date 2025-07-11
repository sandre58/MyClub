// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
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
    internal class RoundViewModel : EntityViewModelBase<Round>, ICompetitionStageViewModel
    {
        private readonly RoundPresentationService _roundPresentationService;
        private readonly ExtendedObservableCollection<IVirtualTeamViewModel> _teams = [];
        private readonly ExtendedObservableCollection<MatchViewModel> _matches = [];
        private readonly ExtendedObservableCollection<RoundStageViewModel> _stages = [];
        private readonly ExtendedObservableCollection<FixtureViewModel> _fixtures = [];

        public RoundViewModel(Round item,
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
            Stages = new(_stages);
            Fixtures = new(_fixtures);
            SchedulingParameters = new SchedulingParametersViewModel(observableSchedulingParameters);

            OpenCommand = CommandsManager.Create(async () => await OpenAsync().ConfigureAwait(false));
            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                item.Fixtures.ToObservableChangeSet()
                             .Transform(x => new FixtureViewModel(x, this, teamsProvider))
                             .ObserveOn(Scheduler.GetUIOrCurrent())
                             .Bind(_fixtures)
                             .DisposeMany()
                             .Subscribe(),
                item.Stages.ToObservableChangeSet()
                           .Transform(x => new RoundStageViewModel(x, this, () => ComputeRoundStageName(x), () => ComputeRoundStageShortName(x), ShowParentName(x), roundPresentationService, matchPresentationService, stadiumsProvider, teamsProvider))
                           .ObserveOn(Scheduler.GetUIOrCurrent())
                           .Bind(_stages)
                           .DisposeMany()
                           .Subscribe(),
                item.Teams.ToObservableChangeSet()
                          .Transform(teamsProvider.GetVirtualTeam)
                          .ObserveOn(Scheduler.GetUIOrCurrent())
                          .Bind(_teams)
                          .Subscribe(),
                _stages.ToObservableChangeSet().MergeManyEx(x => x.Matches.ToObservableChangeSet())
                                               .ObserveOn(Scheduler.GetUIOrCurrent())
                                               .Bind(_matches)
                                               .Subscribe(),
                _matches.ToObservableChangeSet()
                        .WhenPropertyChanged(x => x.Date)
                        .Subscribe(_ =>
                        {
                            Date = Matches.MinOrDefault(m => m.Date);
                            StartDate = Matches.MinOrDefault(m => m.StartDate);
                            EndDate = Matches.MaxOrDefault(m => m.EndDate);
                        }),
                _fixtures.ToObservableChangeSet()
                         .OnItemAdded(x =>
                         {
                            teamsProvider.RegisterVirtualTeam(x.WinnerTeam);
                            teamsProvider.RegisterVirtualTeam(x.LooserTeam);
                         })
                         .OnItemRemoved(x =>
                         {
                            teamsProvider.RemoveVirtualTeam(x.WinnerTeam);
                            teamsProvider.RemoveVirtualTeam(x.LooserTeam);
                         })
                         .Subscribe(),
            ]);
        }

        public IRoundsStageViewModel Stage { get; }

        public SchedulingParametersViewModel SchedulingParameters { get; }

        public string Name => Item.Name;

        public string ShortName => Item.ShortName;

        public DateTime Date { get; private set; }

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        public ReadOnlyObservableCollection<IVirtualTeamViewModel> Teams { get; }

        public ReadOnlyObservableCollection<RoundStageViewModel> Stages { get; }

        public ReadOnlyObservableCollection<FixtureViewModel> Fixtures { get; }

        public ICommand OpenCommand { get; }

        public ICommand EditCommand { get; }

        public bool CanAutomaticReschedule() => Item.ProvideSchedulingParameters().CanAutomaticReschedule();

        public bool CanAutomaticRescheduleVenue() => Item.ProvideSchedulingParameters().CanAutomaticRescheduleVenue();

        public IEnumerable<IVirtualTeamViewModel> GetAvailableTeams() => Teams;

        public async Task OpenAsync() => await _roundPresentationService.OpenAsync(this).ConfigureAwait(false);

        public async Task EditAsync() => await _roundPresentationService.EditAsync(this).ConfigureAwait(false);

        public static string ComputeRoundStageName(RoundStage stage) => stage.Stage.Format switch
        {
            TwoLegsFormat => stage.Stage.Stages.IndexOf(stage) switch
            {
                0 => MyClubResources.FirstLeg,
                1 => MyClubResources.SecondLeg,
                _ => throw new NotSupportedException()
            },
            ReplayFormat => stage.Stage.Stages.IndexOf(stage) switch
            {
                0 => MyClubResources.MainStage,
                1 => MyClubResources.ReplayStage,
                _ => throw new NotSupportedException()
            },
            NumberOfWinsFormat => (stage.Stage.Stages.IndexOf(stage) + 1).ToString(MyClubResources.MatchX),
            OneLegFormat => stage.Stage.Name,
            _ => throw new NotSupportedException()
        };

        public static string ComputeRoundStageShortName(RoundStage stage) => stage.Stage.Format switch
        {
            TwoLegsFormat => stage.Stage.Stages.IndexOf(stage) switch
            {
                0 => MyClubResources.FirstLegAbbr,
                1 => MyClubResources.SecondLegAbbr,
                _ => throw new NotSupportedException()
            },
            ReplayFormat => stage.Stage.Stages.IndexOf(stage) switch
            {
                0 => MyClubResources.MainStageAbbr,
                1 => MyClubResources.ReplayStageAbbr,
                _ => throw new NotSupportedException()
            },
            NumberOfWinsFormat => (stage.Stage.Stages.IndexOf(stage) + 1).ToString(MyClubResources.MatchXAbbr),
            OneLegFormat => stage.Stage.ShortName,
            _ => throw new NotSupportedException()
        };

        private static bool ShowParentName(RoundStage stage) => stage.Stage.Format switch
        {
            TwoLegsFormat => true,
            ReplayFormat => stage.Stage.Stages.IndexOf(stage) > 0,
            NumberOfWinsFormat => true,
            OneLegFormat => false,
            _ => throw new NotSupportedException()
        };
    }
}

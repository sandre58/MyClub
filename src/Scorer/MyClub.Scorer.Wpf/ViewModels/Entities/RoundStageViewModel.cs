// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.UI.Commands;
using MyNet.UI.Threading;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class RoundStageViewModel : EntityViewModelBase<RoundStage>, ICompetitionStageViewModel, IMatchParentViewModel
    {
        private readonly MatchPresentationService _matchPresentationService;
        private readonly RoundPresentationService _roundPresentationService;
        private readonly ExtendedObservableCollection<MatchViewModel> _matches = [];
        private readonly Func<string> _computeName;
        private readonly Func<string> _computeShortName;

        public RoundStageViewModel(RoundStage item,
                                   RoundViewModel stage,
                                   Func<string> computeName,
                                   Func<string> computeShortName,
                                   bool showName,
                                   RoundPresentationService roundPresentationService,
                                   MatchPresentationService matchPresentationService,
                                   StadiumsProvider stadiumsProvider,
                                   TeamsProvider teamsProvider) : base(item)
        {
            _matchPresentationService = matchPresentationService;
            _roundPresentationService = roundPresentationService;

            _computeName = computeName;
            _computeShortName = computeShortName;
            Matches = new(_matches);
            Stage = stage;
            Name = computeName();
            ShortName = computeShortName();
            ShowName = showName;

            OpenCommand = CommandsManager.Create(async () => await OpenAsync().ConfigureAwait(false));
            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            PostponeCommand = CommandsManager.Create(async () => await PostponeAsync().ConfigureAwait(false), CanBePostponed);

            Disposables.AddRange(
            [
                item.Matches.ToObservableChangeSet()
                            .Transform(x => new MatchViewModel(x, this, _matchPresentationService, stadiumsProvider, teamsProvider))
                            .ObserveOn(Scheduler.GetUIOrCurrent())
                            .Bind(_matches)
                            .DisposeMany()
                            .Subscribe(),
                item.WhenPropertyChanged(x => x.Date).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                    RaisePropertyChanged(nameof(Date));
                })
            ]);
        }

        public RoundViewModel Stage { get; }

        IStageViewModel IMatchParentViewModel.Stage => Stage;

        public bool ShowName { get; }

        public string Name { get; private set; }

        public string ShortName { get; private set; }

        public bool IsPostponed => Item.IsPostponed;

        public DateTime Date => Item.Date.ToCurrentTime();

        public ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        public DateTime StartDate => Date;

        public DateTime EndDate => Date.EndOfDay();

        public ICommand OpenCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand PostponeCommand { get; }

        public IEnumerable<IVirtualTeamViewModel> GetAvailableTeams() => Stage.Teams;

        public async Task EditAsync() => await _roundPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task OpenAsync() => await Stage.OpenAsync().ConfigureAwait(false);

        public bool CanAutomaticReschedule() => Stage.CanAutomaticReschedule();

        public bool CanAutomaticRescheduleVenue() => Stage.CanAutomaticRescheduleVenue();

        public bool CanCancelMatch() => true;

        public bool CanBePostponed() => !IsPostponed && !Item.IsPlayed();

        public async Task PostponeAsync() => await _roundPresentationService.PostponeAsync(this).ConfigureAwait(false);

        protected override void OnCultureChanged()
        {
            base.OnCultureChanged();
            Name = _computeName();
            ShortName = _computeShortName();

        }
    }
}

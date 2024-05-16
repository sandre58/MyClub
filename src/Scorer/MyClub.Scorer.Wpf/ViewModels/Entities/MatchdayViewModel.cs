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
using DynamicData.PLinq;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Observable.Threading;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyClub.Scorer.Domain.CompetitionAggregate;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class MatchdayViewModel : EntityViewModelBase<Matchday>, IMatchParent
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly ReadOnlyObservableCollection<MatchViewModel> _matches;
        private readonly TeamsProvider _teamsProvider;

        public MatchdayViewModel(Matchday item,
                                 IMatchdayParent parent,
                                 MatchdayPresentationService matchdayPresentationService,
                                 MatchPresentationService matchPresentationService,
                                 StadiumsProvider stadiumsProvider,
                                 TeamsProvider teamsProvider) : base(item)
        {
            _matchdayPresentationService = matchdayPresentationService;
            _matchPresentationService = matchPresentationService;
            _teamsProvider = teamsProvider;

            Parent = parent;

            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            DuplicateCommand = CommandsManager.Create(async () => await DuplicateAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
            OpenCommand = CommandsManager.Create(async () => await OpenAsync().ConfigureAwait(false));
            PostponeCommand = CommandsManager.Create(async () => await PostponeAsync().ConfigureAwait(false), CanBePostponed);
            AddMatchCommand = CommandsManager.Create(async () => await AddMatchAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                item.Matches.ToObservableChangeSet(x => x.Id)
                            .Transform(x => new MatchViewModel(x, _matchPresentationService, stadiumsProvider, teamsProvider, this))
                            .ObserveOn(Scheduler.UI)
                            .Bind(out _matches)
                            .DisposeMany()
                            .Subscribe(),
                item.WhenPropertyChanged(x => x.Date).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                    RaisePropertyChanged(nameof(MatchTime));
                    RaisePropertyChanged(nameof(DateTime));
                })
            ]);
        }

        public IMatchdayParent Parent { get; set; }

        public string Name => Item.Name;

        public string ShortName => Item.ShortName;

        public bool IsPostponed => Item.IsPostponed;

        public DateTime Date => Item.Date.ToLocalTime();

        public TimeSpan MatchTime => Date.TimeOfDay;

        public DateTime OriginDate => Item.OriginDate;

        public ReadOnlyObservableCollection<MatchViewModel> Matches => _matches;

        public DateTime StartDate => Date.BeginningOfDay();

        public DateTime EndDate => Date.EndOfDay();

        public ICommand OpenCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand DuplicateCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand PostponeCommand { get; }

        public ICommand AddMatchCommand { get; }

        public bool CanEditMatchFormat() => false;

        public bool CanCancelMatch() => false;

        public bool CanBePostponed() => !Item.IsPostponed && Matches.Any(x => !x.IsPlayed);

        public IEnumerable<TeamViewModel> GetAvailableTeams() => _teamsProvider.Items;

        public async Task OpenAsync() => await _matchdayPresentationService.OpenAsync(this).ConfigureAwait(false);

        public async Task EditAsync() => await _matchdayPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task DuplicateAsync() => await _matchdayPresentationService.DuplicateAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _matchdayPresentationService.RemoveAsync([this]).ConfigureAwait(false);

        public async Task PostponeAsync() => await _matchdayPresentationService.PostponeAsync(this).ConfigureAwait(false);

        public async Task AddMatchAsync() => await _matchPresentationService.AddAsync(this).ConfigureAwait(false);
    }
}

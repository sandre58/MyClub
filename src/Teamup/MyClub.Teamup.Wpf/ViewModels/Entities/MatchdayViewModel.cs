// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities;
using MyNet.Observable.Collections;
using MyNet.Observable.Collections.Filters;
using MyNet.Observable.Collections.Sorting;
using MyNet.UI.Threading;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class MatchdayViewModel : EntityViewModelBase<Matchday>, IMatchdayViewModel
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly ExtendedCollection<MatchViewModel> _matches;

        public MatchdayViewModel(Matchday item,
                                 IMatchdayParent parent,
                                 MatchdayPresentationService matchdayPresentationService,
                                 MatchPresentationService matchPresentationService,
                                 StadiumsProvider stadiumsProvider) : base(item)
        {
            _matchdayPresentationService = matchdayPresentationService;
            _matchPresentationService = matchPresentationService;

            Parent = parent;

            _matches = new ExtendedCollection<MatchViewModel>(Scheduler.UI);
            _matches.SortingProperties.Add(new SortingProperty(nameof(MatchViewModel.Date)));
            _matches.Filters.Add(new CompositeFilter(new BooleanFilterViewModel(nameof(MatchViewModel.IsMyMatch)) { Value = true }));

            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            DuplicateCommand = CommandsManager.Create(async () => await DuplicateAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
            OpenCommand = CommandsManager.Create(Open);
            PostponeCommand = CommandsManager.Create(async () => await PostponeAsync().ConfigureAwait(false), () => !Item.IsPostponed);
            AddMatchesCommand = CommandsManager.Create(async () => await AddMatchesAsync().ConfigureAwait(false));
            EditResultsCommand = CommandsManager.Create(async () => await EditResultsAsync().ConfigureAwait(false), () => AllMatches.Any());

            Disposables.AddRange(
            [
                item.Matches.ToObservableChangeSet(x => x.Id)
                            .Transform(x => new MatchViewModel(x, this, stadiumsProvider, matchPresentationService))
                            .OnItemAdded(AddMatch)
                            .OnItemRemoved(RemoveMatch)
                            .DisposeMany()
                            .Subscribe(),
                item.WhenPropertyChanged(x => x.Date).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                })
            ]);
        }

        public Color Color => Parent.Color;

        public IMatchdayParent Parent { get; }

        IMatchParent IMatchParent.Parent => Parent;

        public CompetitionRules Rules => Parent.Rules;

        public string Name => Item.Name;

        public string ShortName => Item.ShortName;

        public bool IsPostponed => Item.IsPostponed;

        public DateTime Date => Item.Date.ToLocalTime();

        public DateTime OriginDate => Item.OriginDate;

        public ReadOnlyObservableCollection<MatchViewModel> Matches => _matches.Items;

        public ReadOnlyObservableCollection<MatchViewModel> AllMatches => _matches.Source;

        public bool HasMatches => AllMatches.Any();

        public DateTime StartDate => Date.BeginningOfDay();

        public DateTime EndDate => Date.EndOfDay();

        public MatchFormat MatchFormat => Item.MatchFormat;

        public ICommand OpenCommand { get; }

        public ICommand AddMatchesCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand EditResultsCommand { get; }

        public ICommand DuplicateCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand PostponeCommand { get; }

        private void AddMatch(MatchViewModel match)
        {
            _matches.Add(match);
            RaisePropertyChanged(nameof(HasMatches));
        }

        private void RemoveMatch(MatchViewModel match)
        {
            _matches.Remove(match);
            RaisePropertyChanged(nameof(HasMatches));
        }

        public void Open() => NavigationCommandsService.NavigateToMatchday(this);

        public async Task EditAsync() => await _matchdayPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task EditResultsAsync() => await _matchPresentationService.EditAsync(AllMatches).ConfigureAwait(false);

        public async Task AddMatchesAsync() => await _matchPresentationService.AddMultipleAsync(this).ConfigureAwait(false);

        public async Task DuplicateAsync() => await _matchdayPresentationService.DuplicateAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _matchdayPresentationService.RemoveAsync([this]).ConfigureAwait(false);

        public async Task PostponeAsync() => await _matchdayPresentationService.PostponeAsync(this).ConfigureAwait(false);

        public IEnumerable<MatchViewModel> GetAllMatches() => AllMatches;

        public DateTime GetDefaultDateTime() => Item.Date.ToLocalDateTime(Parent.GetDefaultDateTime().TimeOfDay);

        public IEnumerable<TeamViewModel> GetAvailableTeams() => Parent.GetAvailableTeams();

        public bool CanCancelMatch() => Parent.CanCancelMatch();

        public bool CanEditMatchFormat() => Parent.CanEditMatchFormat();

        public bool CanEditPenaltyPoints() => Parent.CanEditPenaltyPoints();

    }
}

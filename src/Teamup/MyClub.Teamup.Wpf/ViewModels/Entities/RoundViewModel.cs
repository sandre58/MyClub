// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities;
using MyNet.Observable.Collections;
using MyNet.Observable.Collections.Filters;
using MyNet.Observable.Collections.Sorting;
using MyNet.Observable;
using MyNet.Observable.Threading;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    public enum RoundType
    {
        GroupStage,

        Knockout,
    }

    internal abstract class RoundViewModel : EntityViewModelBase<IRound>, IAppointment, IMatchParent
    {
        private readonly RoundPresentationService _roundPresentationService;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly TeamsProvider _teamsProvider;
        private readonly ExtendedCollection<MatchViewModel> _matches;
        private readonly ReadOnlyObservableCollection<TeamViewModel> _teams;

        protected RoundViewModel(IRound item, CupViewModel parent, RoundPresentationService roundPresentationService, MatchPresentationService matchPresentationService, TeamsProvider teamsProvider) : base(item)
        {
            _roundPresentationService = roundPresentationService;
            _matchPresentationService = matchPresentationService;
            _teamsProvider = teamsProvider;
            Parent = parent;

            _matches = new ExtendedCollection<MatchViewModel>(Scheduler.UI);
            _matches.SortingProperties.Add(new SortingProperty(nameof(MatchViewModel.Date)));
            _matches.Filters.Add(new CompositeFilter(new BooleanFilterViewModel(nameof(MatchViewModel.IsMyMatch)) { Value = true }));

            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
            OpenCommand = CommandsManager.Create(Open);
            EditResultsCommand = CommandsManager.Create(async () => await EditResultsAsync().ConfigureAwait(false), () => AllMatches.Any());

            Disposables.AddRange(
           [
                Item.Teams.ToObservableChangeSet().Transform(x => _teamsProvider.GetOrThrow(x.Id)).ObserveOn(Scheduler.UI).Bind(out _teams).Subscribe(),
            ]);
        }

        public Color Color => Parent.Color;

        public CupViewModel Parent { get; }

        IMatchParent IMatchParent.Parent => Parent;

        public string Name => Item.Name;

        public string ShortName => Item.ShortName;

        public CompetitionRules Rules => Item.Rules;

        public ReadOnlyObservableCollection<MatchViewModel> Matches => _matches.Items;

        public ReadOnlyObservableCollection<MatchViewModel> AllMatches => _matches.Source;

        public bool HasMatches => AllMatches.Any();

        public abstract RoundType Type { get; }

        public ReadOnlyObservableCollection<TeamViewModel> Teams => _teams;

        public abstract DateTime StartDate { get; }

        public abstract DateTime EndDate { get; }

        public ICommand OpenCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand EditResultsCommand { get; }

        public void Open() => NavigationCommandsService.NavigateToRound(this);

        public virtual async Task EditAsync() => await _roundPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _roundPresentationService.RemoveAsync([this]).ConfigureAwait(false);

        protected void AddMatch(MatchViewModel match)
        {
            _matches.Add(match);
            match.ScoreChanged += OnScoreChanged;
            RaisePropertyChanged(nameof(HasMatches));
        }

        protected void RemoveMatch(MatchViewModel match)
        {
            match.ScoreChanged -= OnScoreChanged;
            _matches.Remove(match);
            RaisePropertyChanged(nameof(HasMatches));
        }

        [SuppressPropertyChangedWarnings]
        private void OnScoreChanged(object? sender, EventArgs _) => OnScoreChanged(sender as MatchViewModel);

        [SuppressPropertyChangedWarnings]
        protected virtual void OnScoreChanged(MatchViewModel? matchViewModel) { }

        public IEnumerable<MatchViewModel> GetAllMatches() => AllMatches;

        public async Task EditResultsAsync() => await _matchPresentationService.EditAsync(AllMatches).ConfigureAwait(false);

        public abstract DateTime GetDefaultDateTime();

        public IEnumerable<TeamViewModel> GetAvailableTeams() => Teams;

        public bool CanCancelMatch() => Parent.CanCancelMatch();

        public bool CanEditMatchFormat() => Parent.CanEditMatchFormat();

        public bool CanEditPenaltyPoints() => Parent.CanEditPenaltyPoints();
    }
}

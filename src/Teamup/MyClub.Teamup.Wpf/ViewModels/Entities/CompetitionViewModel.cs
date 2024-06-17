// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using MyNet.UI.Threading;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    public enum CompetitionType
    {
        League,

        Cup,

        Friendly,
    }

    internal abstract class CompetitionViewModel : EntityViewModelBase<ICompetitionSeason>, IMatchParent, ISearchableItem
    {
        private readonly TeamsProvider _teamsProvider;
        private readonly ExtendedCollection<MatchViewModel> _matches;
        private readonly ReadOnlyObservableCollection<TeamViewModel> _teams;

        protected CompetitionViewModel(ICompetitionSeason item, TeamsProvider teamsProvider) : base(item)
        {
            _teamsProvider = teamsProvider;

            Base = item.Competition;

            _matches = new ExtendedCollection<MatchViewModel>(Scheduler.UI);
            _matches.SortingProperties.Add(new SortingProperty(nameof(MatchViewModel.Date)));
            _matches.Filters.Add(new CompositeFilter(new BooleanFilterViewModel(nameof(MatchViewModel.IsMyMatch)) { Value = true }));

            OpenCommand = CommandsManager.Create(Open);
            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
            DuplicateCommand = CommandsManager.Create(async () => await DuplicateAsync().ConfigureAwait(false));

            var matchesObservable = _matches.Connect();
            Disposables.AddRange(
           [
                Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(x => Base.PropertyChanged += x, x => Base.PropertyChanged -= x).Subscribe(x => RaisePropertyChanged(x.EventArgs.PropertyName)),
                Item.Teams.ToObservableChangeSet().Transform(x => _teamsProvider.GetOrThrow(x.Id)).ObserveOn(Scheduler.UI).Bind(out _teams).Subscribe(),
                Item.Period.WhenAnyPropertyChanged().Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                    RaisePropertyChanged(nameof(IsEnded));
                    RaisePropertyChanged(nameof(IsStarted));
                    RaisePropertyChanged(nameof(IsCurrent));
                }),
                matchesObservable.WhenPropertyChanged(x => x.Date).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(NextMatch));
                    RaisePropertyChanged(nameof(PreviousMatch));
                }),
                matchesObservable.Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(NextMatch));
                    RaisePropertyChanged(nameof(PreviousMatch));
                })
            ]);
        }

        public abstract Color Color { get; }

        protected ICompetition Base { get; }

        public IMatchParent? Parent => null;

        [AlsoNotifyFor(nameof(SearchDisplayName))]
        public string Name => Base.Name;

        public string ShortName => Base.ShortName;

        public Category Category => Base.Category;

        public byte[]? Logo => Base.Logo;

        public DateTime StartDate => Item.Period.Start.Date;

        public DateTime EndDate => Item.Period.End.Date;

        public CompetitionRules Rules => Item.Rules;

        public bool IsEnded => Item.Period.End.IsInPast();

        public bool IsStarted => Item.Period.Start.IsInPast();

        public bool IsCurrent => IsStarted && !IsEnded;

        public ReadOnlyObservableCollection<MatchViewModel> Matches => _matches.Items;

        public ReadOnlyObservableCollection<MatchViewModel> AllMatches => _matches.Source;

        public abstract CompetitionType Type { get; }

        public ReadOnlyObservableCollection<TeamViewModel> Teams => _teams;

        public MatchViewModel? NextMatch => Matches.OrderBy(x => x.Date).FirstOrDefault(x => x.Date.IsInFuture());

        public MatchViewModel? PreviousMatch => Matches.OrderBy(x => x.Date).LastOrDefault(x => x.Date.IsInPast());

        public ICommand EditCommand { get; }

        public ICommand DuplicateCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand OpenCommand { get; }

        public abstract Task EditAsync();

        public abstract Task RemoveAsync();

        public abstract Task DuplicateAsync();

        protected void AddMatch(MatchViewModel match)
        {
            _matches.Add(match);
            match.ScoreChanged += OnScoreChanged;
        }

        protected void RemoveMatch(MatchViewModel match)
        {
            match.ScoreChanged -= OnScoreChanged;
            _matches.Remove(match);
        }

        public void Open() => NavigationCommandsService.NavigateToCompetitionPage(this);

        [SuppressPropertyChangedWarnings]
        private void OnScoreChanged(object? sender, EventArgs _) => OnScoreChanged(sender as MatchViewModel);

        [SuppressPropertyChangedWarnings]
        protected virtual void OnScoreChanged(MatchViewModel? matchViewModel) { }

        public IEnumerable<MatchViewModel> GetAllMatches() => AllMatches;

        public DateTime GetDefaultDateTime() => (StartDate.IsInPast() ? DateTime.Today : StartDate).ToLocalDateTime(Rules.MatchTime);

        public IEnumerable<TeamViewModel> GetAvailableTeams() => Teams;

        public abstract bool CanCancelMatch();

        public abstract bool CanEditMatchFormat();

        public abstract bool CanEditPenaltyPoints();

        #region ISearchableItem

        public string SearchDisplayName => Name;

        public string SearchText => Name;

        public string SearchCategory => nameof(MyClubResources.Competitions);

        #endregion
    }
}

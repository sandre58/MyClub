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
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class GroupStageViewModel : RoundViewModel, IMatchdayParent
    {
        private readonly UiObservableCollection<MatchdayViewModel> _matchdays = [];
        private readonly UiObservableCollection<GroupViewModel> _groups = [];
        private readonly MatchdayPresentationService _matchdayPresentationService;

        public GroupStageViewModel(GroupStage item,
                                   CupViewModel parent,
                                   RoundPresentationService roundPresentationService,
                                   MatchdayPresentationService matchdayPresentationService,
                                   MatchPresentationService matchPresentationService,
                                   TeamsProvider teamsProvider,
                                   StadiumsProvider stadiumsProvider)
            : base(item, parent, roundPresentationService, matchPresentationService, teamsProvider)
        {
            _matchdayPresentationService = matchdayPresentationService;
            Groups = new(_groups);
            Matchdays = new(_matchdays);

            AddMatchdaysCommand = CommandsManager.Create(async () => await AddMatchdaysAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                item.Groups.ToObservableChangeSet(x => x.Id)
                              .Transform(x => new GroupViewModel(x, this, teamsProvider))
                              .SortBy(x => x.Name)
                              .AutoRefresh(x => x.Name)
                              .BindItems(_groups)
                              .DisposeMany()
                              .Subscribe(_ => MyGroup = _groups.FirstOrDefault(y => y.Teams.Any(z => z.IsMyTeam))),
                item.Matchdays.ToObservableChangeSet(x => x.Id)
                              .Transform(x => new MatchdayViewModel(x, this, matchdayPresentationService, matchPresentationService, stadiumsProvider))
                              .SortBy(x => x.Date)
                              .AutoRefresh(x => x.Date)
                              .BindItems(_matchdays)
                              .DisposeMany()
                              .Subscribe(_ =>
                              {
                                  RaisePropertyChanged(nameof(PreviousMatchday));
                                  RaisePropertyChanged(nameof(NextMatchday));
                              }),
                _matchdays.ToObservableChangeSet()
                          .MergeManyEx(x => x.AllMatches.ToObservableChangeSet())
                          .OnItemAdded(AddMatch)
                          .OnItemRemoved(RemoveMatch)
                          .Subscribe(),
                _groups.ToObservableChangeSet().MergeManyEx(x => x.Teams.ToObservableChangeSet()).Subscribe(_ => MyGroup = _groups.FirstOrDefault(y => y.Teams.Any(z => z.IsMyTeam))),
                item.Period.WhenAnyPropertyChanged().Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                })
            ]);
        }

        public override RoundType Type => RoundType.GroupStage;

        public override DateTime StartDate => Item.CastIn<GroupStage>().Period.Start;

        public override DateTime EndDate => Item.CastIn<GroupStage>().Period.End;

        public RankingRules RankingRules => Rules.CastIn<ChampionshipRules>().RankingRules;

        public ReadOnlyObservableCollection<GroupViewModel> Groups { get; }

        public ReadOnlyObservableCollection<MatchdayViewModel> Matchdays { get; }

        public GroupViewModel? MyGroup { get; private set; }

        public ICommand AddMatchdaysCommand { get; }

        public MatchdayViewModel? NextMatchday => Matchdays.OrderBy(x => x.Date).FirstOrDefault(x => x.Date.IsInFuture());

        public MatchdayViewModel? PreviousMatchday => Matchdays.OrderBy(x => x.Date).LastOrDefault(x => x.Date.IsInPast());

        public async Task AddMatchdaysAsync() => await _matchdayPresentationService.AddMultipleAsync(this, [DateTime.Today]).ConfigureAwait(false);

        public IEnumerable<IMatchdayViewModel> GetAllMatchdays() => Matchdays;

        public override DateTime GetDefaultDateTime() => (StartDate.IsInPast() ? DateTime.Today : StartDate).ToLocalDateTime(Parent.GetDefaultDateTime().TimeOfDay);

    }
}

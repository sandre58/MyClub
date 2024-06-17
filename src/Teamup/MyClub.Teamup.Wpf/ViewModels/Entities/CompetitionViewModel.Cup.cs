// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyNet.DynamicData.Extensions;
using MyNet.UI.Collections;
using MyNet.Utilities;
using MyNet.Wpf.Helpers;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class CupViewModel : CompetitionViewModel
    {
        private readonly CupPresentationService _cupPresentationService;
        private readonly UiObservableCollection<RoundViewModel> _rounds = [];

        public CupViewModel(CupSeason item,
                            CupPresentationService cupPresentationService,
                            RoundPresentationService roundPresentationService,
                            MatchdayPresentationService matchdayPresentationService,
                            MatchPresentationService matchPresentationService,
                            TeamsProvider teamsProvider,
                            StadiumsProvider stadiumsProvider)
            : base(item, teamsProvider)
        {
            _cupPresentationService = cupPresentationService;
            Rounds = new(_rounds);

            Disposables.AddRange(
            [
                item.Rounds.ToObservableChangeSet(x => x.Id)
                           .Transform(x =>
                           {
                               RoundViewModel round = x switch
                               {
                                   Knockout knockout => new KnockoutViewModel(knockout, this, roundPresentationService, matchPresentationService, teamsProvider, stadiumsProvider),
                                   GroupStage groupStage => new GroupStageViewModel(groupStage, this, roundPresentationService, matchdayPresentationService, matchPresentationService, teamsProvider, stadiumsProvider),
                                   _ => throw new InvalidOperationException("Round type is unknown")
                               };

                               return round;
                           })
                           .BindItems(_rounds)
                           .DisposeMany()
                           .Subscribe(_ => RaisePropertyChanged(nameof(CurrentRound))),
                _rounds.ToObservableChangeSet()
                       .MergeManyEx(x => x.AllMatches.ToObservableChangeSet())
                       .OnItemAdded(AddMatch)
                       .OnItemRemoved(RemoveMatch)
                       .Subscribe(),
                _rounds.ToObservableChangeSet()
                       .WhenAnyPropertyChanged(nameof(StartDate), nameof(EndDate))
                       .Subscribe(_ => RaisePropertyChanged(nameof(CurrentRound))),
            ]);
        }

        public override Color Color => WpfHelper.GetResource<Color>("Teamup.Colors.Competition.Cup");

        public override CompetitionType Type => CompetitionType.Cup;

        public ReadOnlyObservableCollection<RoundViewModel> Rounds { get; }

        public RoundViewModel? CurrentRound => Rounds.OrderBy(x => x.EndDate).LastOrDefault(x => x.StartDate.IsInPast() && x.EndDate.IsInFuture() || x.EndDate.IsInPast());

        public override bool CanCancelMatch() => false;

        public override bool CanEditMatchFormat() => false;

        public override bool CanEditPenaltyPoints() => false;

        public override async Task EditAsync() => await _cupPresentationService.EditAsync(this).ConfigureAwait(false);

        public override async Task DuplicateAsync() => await _cupPresentationService.DuplicateAsync(this).ConfigureAwait(false);

        public override async Task RemoveAsync() => await _cupPresentationService.RemoveAsync([this]).ConfigureAwait(false);
    }
}

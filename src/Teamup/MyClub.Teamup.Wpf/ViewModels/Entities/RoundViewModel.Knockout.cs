// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class KnockoutViewModel : RoundViewModel, IMatchdayViewModel
    {
        private readonly RoundPresentationService _roundPresentationService;
        private readonly MatchPresentationService _matchPresentationService;

        public KnockoutViewModel(Knockout item,
                                 CupViewModel cup,
                                 RoundPresentationService roundPresentationService,
                                 MatchPresentationService matchPresentationService,
                                 TeamsProvider teamsProvider,
                                 StadiumsProvider stadiumsProvider)
            : base(item, cup, roundPresentationService, matchPresentationService, teamsProvider)
        {
            _roundPresentationService = roundPresentationService;
            _matchPresentationService = matchPresentationService;

            PostponeCommand = CommandsManager.Create(async () => await PostponeAsync().ConfigureAwait(false), () => !Item.CastIn<Knockout>().IsPostponed);
            AddMatchesCommand = CommandsManager.Create(async () => await AddMatchesAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                item.WhenPropertyChanged(x => x.Date).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                }),
                item.Matches.ToObservableChangeSet(x => x.Id)
                            .Transform(x => new MatchViewModel(x, this, stadiumsProvider, matchPresentationService))
                            .OnItemAdded(AddMatch)
                            .OnItemRemoved(RemoveMatch)
                            .DisposeMany()
                            .Subscribe()
            ]);
        }

        public override RoundType Type => RoundType.Knockout;

        public bool IsPostponed => Item.CastIn<Knockout>().IsPostponed;

        public DateTime Date => Item.CastIn<Knockout>().Date.ToLocalTime();

        public DateTime OriginDate => Item.CastIn<Knockout>().OriginDate;

        public override DateTime StartDate => Date.BeginningOfDay();

        public override DateTime EndDate => Date.EndOfDay();

        public ICommand AddMatchesCommand { get; }

        public ICommand PostponeCommand { get; }

        public async Task AddMatchesAsync() => await _matchPresentationService.AddMultipleAsync(this).ConfigureAwait(false);

        public async Task PostponeAsync() => await _roundPresentationService.PostponeAsync(this).ConfigureAwait(false);

        public override DateTime GetDefaultDateTime() => Date.ToLocal(Parent.GetDefaultDateTime().TimeOfDay);
    }
}

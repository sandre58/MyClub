// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.Utilities;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Misc;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class GroupStageDetailsViewModel : RoundDetailsViewModel
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;
        private readonly MatchPresentationService _matchPresentationService;

        public GroupStageDetailsViewModel(GroupStageViewModel item, MatchdayPresentationService matchdayPresentationService, MatchPresentationService matchPresentationService)
            : base(item)
        {
            _matchdayPresentationService = matchdayPresentationService;
            _matchPresentationService = matchPresentationService;

            MatchesViewModel = new(item, matchPresentationService, new GroupStageMatchesListParametersProvider(item));

            AddMatchesCommand = CommandsManager.CreateNotNull<MatchdayViewModel>(async x => await AddMatchesAsync(x).ConfigureAwait(false));
            AddMatchdayCommand = CommandsManager.Create(async () => await AddMatchdayAsync().ConfigureAwait(false));
            AddMatchdaysCommand = CommandsManager.Create(async () => await AddMatchdaysAsync().ConfigureAwait(false));
            ShowRankingDetailsCommand = CommandsManager.CreateNotNull<GroupViewModel>(async x =>
            {
                var vm = new RankingDetailsDialogViewModel(x.Ranking, Item.Rules.CastIn<ChampionshipRules>().RankingRules, x.Penalties);
                await DialogManager.ShowAsync(vm).ConfigureAwait(false);
            });
        }

        public MatchesViewModel MatchesViewModel { get; private set; }

        public ICommand ShowRankingDetailsCommand { get; }

        public ICommand AddMatchesCommand { get; }

        public ICommand AddMatchdayCommand { get; }

        public ICommand AddMatchdaysCommand { get; }

        public async Task AddMatchdayAsync() => await _matchdayPresentationService.AddAsync(Item.CastIn<GroupStageViewModel>(), DateTime.Today).ConfigureAwait(false);

        public async Task AddMatchdaysAsync() => await _matchdayPresentationService.AddMultipleAsync(Item.CastIn<GroupStageViewModel>(), [DateTime.Today]).ConfigureAwait(false);

        public async Task AddMatchesAsync(MatchdayViewModel matchdayViewModel) => await _matchPresentationService.AddMultipleAsync(matchdayViewModel).ConfigureAwait(false);
    }
}

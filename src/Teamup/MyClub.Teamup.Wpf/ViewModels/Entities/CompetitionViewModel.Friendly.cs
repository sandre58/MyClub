// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using MyNet.Wpf.Helpers;
using MyNet.Utilities;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class FriendlyViewModel : CompetitionViewModel
    {
        private readonly FriendlyPresentationService _friendlyPresentationService;

        public FriendlyViewModel(FriendlySeason item,
                                 FriendlyPresentationService friendlyPresentationService,
                                 MatchPresentationService matchPresentationService,
                                 TeamsProvider teamsProvider,
                                 StadiumsProvider stadiumsProvider)
            : base(item, teamsProvider)
        {
            _friendlyPresentationService = friendlyPresentationService;

            Disposables.AddRange(
            [
                item.Matches.ToObservableChangeSet(x => x.Id)
                            .Transform(x => new MatchViewModel(x, this, stadiumsProvider, matchPresentationService))
                            .OnItemAdded(AddMatch)
                            .OnItemRemoved(RemoveMatch)
                            .DisposeMany()
                            .Subscribe(),
            ]);
        }

        public override Color Color => WpfHelper.GetResource<Color>("Teamup.Colors.Competition.Friendly");

        public override CompetitionType Type => CompetitionType.Friendly;

        public override bool CanCancelMatch() => true;

        public override bool CanEditMatchFormat() => true;

        public override bool CanEditPenaltyPoints() => false;

        public override async Task EditAsync() => await _friendlyPresentationService.EditAsync(this).ConfigureAwait(false);

        public override async Task DuplicateAsync() => await _friendlyPresentationService.DuplicateAsync(this).ConfigureAwait(false);

        public override async Task RemoveAsync() => await _friendlyPresentationService.RemoveAsync([this]).ConfigureAwait(false);
    }
}

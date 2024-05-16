// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class KnockoutDetailsViewModel : RoundDetailsViewModel
    {
        public KnockoutDetailsViewModel(KnockoutViewModel item, MatchPresentationService matchPresentationService)
            : base(item) => MatchesViewModel = new(item, matchPresentationService, new KnockoutMatchesListParametersProvider(item));

        public MatchesViewModel MatchesViewModel { get; private set; }
    }
}

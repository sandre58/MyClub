// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive.Subjects;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class FriendlyPageMatchesViewModel : MatchesViewModel
    {
        public FriendlyPageMatchesViewModel(MatchPresentationService matchPresentationService, Subject<FriendlyViewModel?> friendlyChanged)
            : base(new MatchesByCompetitionSourceProvider<FriendlyViewModel>(friendlyChanged), matchPresentationService, new FriendlyMatchesListParametersProvider(friendlyChanged))
            => Disposables.Add(friendlyChanged.Subscribe(x => Parent = x));
    }
}

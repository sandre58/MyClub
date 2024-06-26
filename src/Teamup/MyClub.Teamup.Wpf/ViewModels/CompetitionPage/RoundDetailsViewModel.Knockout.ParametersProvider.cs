﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class KnockoutMatchesListParametersProvider(KnockoutViewModel knockout) : ListParametersProvider(nameof(MatchViewModel.Date))
    {
        private readonly KnockoutViewModel _knockout = knockout;

        public override IFiltersViewModel ProvideFilters() => new KnockoutMatchesSpeedFiltersViewModel(_knockout);
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Wpf.Filters;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities.Comparaison;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class PlayerImportablesListParametersProvider() : ListParametersProvider(nameof(PlayerImportableViewModel.LastName))
    {
        public override IFiltersViewModel ProvideFilters() => new PlayerImportablesSpeedFiltersViewModel();
    }

    internal class PlayerImportablesSpeedFiltersViewModel : SpeedFiltersViewModel
    {

        public PlayerImportablesSpeedFiltersViewModel() => AddRange([NameFilter, CountryFilter, GenderFilter, AgeFilter]);

        public StringFilterViewModel NameFilter { get; } = new(nameof(PlayerImportableViewModel.LastName));

        public CountryFilterViewModel CountryFilter { get; } = new(nameof(PlayerImportableViewModel.Country));

        public GenderFilterViewModel GenderFilter { get; } = new GenderFilterViewModel(nameof(PlayerImportableViewModel.Gender)) { IsReadOnly = true };

        public IntegerFilterViewModel AgeFilter { get; } = new(nameof(PlayerImportableViewModel.Age), ComplexComparableOperator.IsBetween, Player.AcceptableRangeAge);
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable.Translatables;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.UI.ViewModels.List.Grouping;
using MyNet.UI.ViewModels.List.Sorting;
using MyNet.Utilities.Comparaison;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Wpf.ViewModels.RosterPage
{
    internal class PlayersListParametersProvider : ListParametersProvider
    {
        public override IFiltersViewModel ProvideFilters() => new ExtendedFiltersViewModel(new Dictionary<string, Func<IFilterViewModel>>
            {
                { nameof(MyClubResources.Name), () => new StringFilterViewModel(nameof(PlayerViewModel.InverseName)) },
                { nameof(MyClubResources.Category), () => new EnumerationValuesFilterViewModel(nameof(PlayerViewModel.Category), typeof(Category)) },
                { nameof(MyClubResources.Age), () => new IntegerFilterViewModel(nameof(PlayerViewModel.Age), ComplexComparableOperator.IsBetween, Player.AcceptableRangeAge) },
                { nameof(MyClubResources.Number), () => new IntegerFilterViewModel(nameof(PlayerViewModel.Number), ComplexComparableOperator.IsBetween, SquadPlayer.AcceptableRangeNumber) },
                { nameof(MyClubResources.IsAbsent), () => new BooleanFilterViewModel(nameof(PlayerViewModel.IsAbsent)) },
                { nameof(MyClubResources.IsInjured), () => new BooleanFilterViewModel(nameof(PlayerViewModel.IsInjured)) },
                { nameof(MyClubResources.LicenseState), () => new EnumValuesFilterViewModel(nameof(PlayerViewModel.LicenseState), typeof(LicenseState)) },
                { nameof(MyClubResources.IsMutation), () => new BooleanFilterViewModel(nameof(PlayerViewModel.IsMutation)) },
                { nameof(MyClubResources.Positions), () => new EnumerationValuesFilterViewModel(nameof(PlayerViewModel.NaturalPosition), typeof(Position)) },
                { nameof(MyClubResources.Position), () => new RatedPositionFilterViewModel(nameof(PlayerViewModel.Positions)) },
                { nameof(MyClubResources.Gender), () => new GenderFilterViewModel(nameof(PlayerViewModel.Gender)) },
                { nameof(MyClubResources.FromDate), () => new DateFilterViewModel(nameof(PlayerViewModel.FromDate), ComplexComparableOperator.LessThan, DateTime.Now, DateTime.Now) },
                { nameof(MyClubResources.Country), () => new CountriesFilterViewModel(nameof(PlayerViewModel.Country)) },
                { nameof(MyClubResources.City), () => new StringFilterViewModel($"{nameof(PlayerViewModel.Address)}.{nameof(Address.City)}") },
                { nameof(MyClubResources.Laterality), () => new EnumValuesFilterViewModel(nameof(PlayerViewModel.Laterality), typeof(Laterality)) },
                { nameof(MyClubResources.Height), () => new IntegerFilterViewModel(nameof(PlayerViewModel.Height), ComplexComparableOperator.IsBetween, Player.AcceptableRangeHeight) },
                { nameof(MyClubResources.Weight), () => new IntegerFilterViewModel(nameof(PlayerViewModel.Weight), ComplexComparableOperator.IsBetween, Player.AcceptableRangeWeight) },
                { nameof(MyClubResources.Size), () => new StringFilterViewModel(nameof(PlayerViewModel.Size)) },
                { nameof(MyClubResources.ShoesSize), () => new IntegerFilterViewModel(nameof(PlayerViewModel.ShoesSize), ComplexComparableOperator.IsBetween, SquadPlayer.AcceptableRangeShoesSize) }
            }, new PlayersSpeedFiltersViewModel());

        public override IGroupingViewModel ProvideGrouping()
            => new ExtendedGroupingViewModel(
            [
                new GroupingPropertyViewModel(nameof(MyClubResources.Position), $"{nameof(PlayerViewModel.NaturalPosition)}.Type", nameof(PlayerViewModel.NaturalPosition)),
                new GroupingPropertyViewModel(nameof(MyClubResources.Name), nameof(PlayerViewModel.FirstLetter), null),
                new GroupingPropertyViewModel(nameof(MyClubResources.Category), nameof(PlayerViewModel.Category), null),
                new GroupingPropertyViewModel(nameof(MyClubResources.Team), nameof(PlayerViewModel.Team), null),
                new GroupingPropertyViewModel(nameof(MyClubResources.Country), nameof(PlayerViewModel.Country), null),
                new GroupingPropertyViewModel(nameof(MyClubResources.BirthYear), $"{nameof(PlayerViewModel.Birthdate)}.Year", null),
            ]);

        public override ISortingViewModel ProvideSorting()
            => new ExtendedSortingViewModel(new Dictionary<string, string>
            {
                { nameof(MyClubResources.Team), nameof(PlayerViewModel.Team) },
                { nameof(MyClubResources.Name), nameof(PlayerViewModel.InverseName) },
                { nameof(MyClubResources.Position), nameof(PlayerViewModel.NaturalPosition) },
                { nameof(MyClubResources.Number), nameof(PlayerViewModel.Number) },
                { nameof(MyClubResources.Category), nameof(PlayerViewModel.Category) },
                { nameof(MyClubResources.Gender), nameof(PlayerViewModel.Gender) },
                { nameof(MyClubResources.Age), nameof(PlayerViewModel.Age) },
                { nameof(MyClubResources.Birthdate), nameof(PlayerViewModel.Birthdate) },
                { nameof(MyClubResources.Absent), nameof(PlayerViewModel.IsAbsent) },
                { nameof(MyClubResources.Injured), nameof(PlayerViewModel.IsInjured) },
                { nameof(MyClubResources.Country), nameof(PlayerViewModel.Country) },
                { nameof(MyClubResources.LicenseNumber), nameof(PlayerViewModel.LicenseNumber) },
                { nameof(MyClubResources.LicenseState), nameof(PlayerViewModel.LicenseState) },
                { nameof(MyClubResources.InClubFromDate), nameof(PlayerViewModel.FromDate) },
                { nameof(MyClubResources.City), $"{nameof(PlayerViewModel.Address)}.{nameof(Address.City)}" },
                { nameof(MyClubResources.Laterality), nameof(PlayerViewModel.Laterality) },
                { nameof(MyClubResources.Height), nameof(PlayerViewModel.Height) },
                { nameof(MyClubResources.Weight), nameof(PlayerViewModel.Weight) },
                { nameof(MyClubResources.Size), nameof(PlayerViewModel.Size) },
                { nameof(MyClubResources.ShoesSize), nameof(PlayerViewModel.ShoesSize) }
            },
                new[] { nameof(PlayerViewModel.InverseName) });

        public override IDisplayViewModel ProvideDisplay()
        {
            var generalInformationColumns = new[] { nameof(PlayerViewModel.Team), nameof(PlayerViewModel.Age), nameof(PlayerViewModel.Birthdate), nameof(PlayerViewModel.Category), nameof(PlayerViewModel.Gender), nameof(PlayerViewModel.Country), nameof(PlayerViewModel.Address), nameof(PlayerViewModel.Phone), nameof(PlayerViewModel.Email) };
            var clubInformationColumns = new[] { nameof(PlayerViewModel.Team), nameof(PlayerViewModel.IsInjured), nameof(PlayerViewModel.Number), nameof(PlayerViewModel.Age), nameof(PlayerViewModel.Category), nameof(PlayerViewModel.Gender), nameof(PlayerViewModel.Country), nameof(PlayerViewModel.NaturalPosition), nameof(PlayerViewModel.LicenseNumber), nameof(PlayerViewModel.LicenseState), nameof(PlayerViewModel.FromDate) };
            var bodyInformationColumns = new[] { nameof(PlayerViewModel.Age), nameof(PlayerViewModel.Laterality), nameof(PlayerViewModel.Height), nameof(PlayerViewModel.Weight), nameof(PlayerViewModel.Size), nameof(PlayerViewModel.ShoesSize) };

            var modeList = new DisplayModeList(generalInformationColumns);
            modeList.PresetColumns.Add(new DisplayWrapper<string[]>(generalInformationColumns, nameof(MyClubResources.General)));
            modeList.PresetColumns.Add(new DisplayWrapper<string[]>(clubInformationColumns, nameof(MyClubResources.Club)));
            modeList.PresetColumns.Add(new DisplayWrapper<string[]>(bodyInformationColumns, nameof(MyClubResources.Morphology)));

            return new DisplayViewModel().AddMode<DisplayModeGrid>(true).AddMode(modeList);
        }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.UI.ViewModels.List.Sorting;
using MyNet.Utilities.Comparaison;
using MyNet.Utilities.Units;

namespace MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.InjuriesTab
{
    internal class InjuriesListParametersProvider : ListParametersProvider
    {
        public override IFiltersViewModel ProvideFilters() => new ExtendedFiltersViewModel(
            new Dictionary<string, Func<IFilterViewModel>>
                {
                    { nameof(MyClubResources.CurrentlyInjured), () => new BooleanFilterViewModel($"{nameof(InjuryViewModel.IsCurrent)}") },
                    { nameof(MyClubResources.Date), () => new DateFilterViewModel(nameof(InjuryViewModel.Date), ComplexComparableOperator.LessThan, DateTime.Now, DateTime.Now) },
                    { nameof(MyClubResources.EndDate), () => new DateFilterViewModel(nameof(InjuryViewModel.EndDate), ComplexComparableOperator.GreaterThan, DateTime.Now, DateTime.Now) },
                    { nameof(MyClubResources.Duration), () => new TimeSpanFilterViewModel(nameof(InjuryViewModel.Duration), ComplexComparableOperator.GreaterThan, 1, 2, TimeUnit.Month) },
                    { nameof(MyClubResources.InjuryCategory), () => new EnumValuesFilterViewModel<InjuryCategory>(nameof(InjuryViewModel.Category)) },
                    { nameof(MyClubResources.Type), () => new EnumValuesFilterViewModel<InjuryType>(nameof(InjuryViewModel.Type)) },
                    { nameof(MyClubResources.Condition), () => new StringFilterViewModel(nameof(InjuryViewModel.Condition)) },
                    { nameof(MyClubResources.Severity), () => new EnumValuesFilterViewModel<InjurySeverity>(nameof(InjuryViewModel.Severity)) },
                    { nameof(MyClubResources.Team), () => new TeamsFilterViewModel($"{nameof(InjuryViewModel.Player)}.{nameof(PlayerViewModel.Team)}", string.Empty) },
                    { nameof(MyClubResources.Name), () => new StringFilterViewModel($"{nameof(InjuryViewModel.Player)}.{nameof(PlayerViewModel.InverseName)}") },
                    { nameof(MyClubResources.Gender), () => new GenderFilterViewModel($"{nameof(InjuryViewModel.Player)}.{nameof(PlayerViewModel.Gender)}") },
                    { nameof(MyClubResources.Category), () => new EnumerationValuesFilterViewModel($"{nameof(InjuryViewModel.Player)}.{nameof(PlayerViewModel.Category)}", typeof(Category)) },
                    { nameof(MyClubResources.Age), () => new IntegerFilterViewModel($"{nameof(InjuryViewModel.Player)}.{nameof(PlayerViewModel.Age)}", ComplexComparableOperator.IsBetween, Player.AcceptableRangeAge) },
                    { nameof(MyClubResources.Positions), () => new EnumerationValuesFilterViewModel($"{nameof(InjuryViewModel.Player)}.{nameof(PlayerViewModel.NaturalPosition)}", typeof(Position)) }
                }, new InjuriesSpeedFiltersViewModel());

        public override ISortingViewModel ProvideSorting()
            => new ExtendedSortingViewModel(new Dictionary<string, string>
            {
                { nameof(MyClubResources.Date), nameof(InjuryViewModel.Date) },
                { nameof(MyClubResources.ReturnDate), nameof(InjuryViewModel.EndDateOrMax) },
                { nameof(MyClubResources.Duration), nameof(InjuryViewModel.Duration) },
                { nameof(MyClubResources.Category), nameof(InjuryViewModel.Category) },
                { nameof(MyClubResources.Type), nameof(InjuryViewModel.Type) },
                { nameof(MyClubResources.Condition), nameof(InjuryViewModel.Condition) },
                { nameof(MyClubResources.Severity), nameof(InjuryViewModel.Severity) },
                { nameof(MyClubResources.Player), nameof(InjuryViewModel.Player) }
            },
                new Dictionary<string, ListSortDirection> { { nameof(InjuryViewModel.EndDateOrMax), ListSortDirection.Descending } });

        public override IDisplayViewModel ProvideDisplay()
            => new DisplayViewModel().AddMode<DisplayModeGrid>(true).AddMode<DisplayModeList>();
    }
}

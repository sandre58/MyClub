// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities.Comparaison;
using MyNet.Utilities.Units;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.InjuriesTab
{
    internal class InjuriesSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public InjuriesSpeedFiltersViewModel() => AddRange([DurationFilter, NameFilter, CategoryFilter, DateFilter, TeamsFilter, GenderFilter, IsCurrentlyInjured]);

        public TimeSpanFilterViewModel DurationFilter { get; } = new(nameof(InjuryViewModel.Duration), ComplexComparableOperator.GreaterThan, 1, 2, TimeUnit.Month);

        public StringFilterViewModel NameFilter { get; } = new($"{nameof(InjuryViewModel.Player)}.{nameof(PlayerViewModel.InverseName)}");

        public EnumValuesFilterViewModel<InjuryCategory> CategoryFilter { get; } = new(nameof(InjuryViewModel.Category));

        public DateFilterViewModel DateFilter { get; } = new(nameof(InjuryViewModel.Date), ComplexComparableOperator.IsBetween, DateTime.Now, DateTime.Now);

        public TeamsFilterViewModel TeamsFilter { get; } = new TeamsFilterViewModel($"{nameof(InjuryViewModel.Player)}.{nameof(PlayerViewModel.TeamId)}");

        public GenderFilterViewModel GenderFilter { get; } = new GenderFilterViewModel($"{nameof(InjuryViewModel.Player)}.{nameof(PlayerViewModel.Gender)}") { IsReadOnly = true };

        public BooleanFilterViewModel IsCurrentlyInjured { get; } = new($"{nameof(InjuryViewModel.IsCurrent)}") { AllowNullValue = true };
    }
}

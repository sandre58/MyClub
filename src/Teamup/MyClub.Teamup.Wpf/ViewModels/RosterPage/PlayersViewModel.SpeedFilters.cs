// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities.Comparaison;
using MyNet.Observable.Attributes;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.RosterPage
{
    internal class PlayersSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public PlayersSpeedFiltersViewModel() => AddRange([NameFilter, AgeFilter, TeamsFilter, GenderFilter, PositionsFilter, CountryFilter]);

        [UpdateOnCultureChanged]
        public static IReadOnlyCollection<EditableRatedPositionViewModel> Positions { get; } = Position.GetPlayerPositions().Select(x => new EditableRatedPositionViewModel(x) { Rating = PositionRating.Inefficient }).ToArray();

        public StringFilterViewModel NameFilter { get; } = new(nameof(PlayerViewModel.InverseName));

        public IntegerFilterViewModel AgeFilter { get; } = new(nameof(PlayerViewModel.Age), ComplexComparableOperator.IsBetween, Player.AcceptableRangeAge);

        public TeamsFilterViewModel TeamsFilter { get; } = new TeamsFilterViewModel(nameof(PlayerViewModel.TeamId));

        public GenderFilterViewModel GenderFilter { get; } = new GenderFilterViewModel(nameof(PlayerViewModel.Gender)) { IsReadOnly = true };

        public SelectedValuesFilterViewModel<EditableRatedPositionViewModel> PositionsFilter { get; } = new(nameof(PlayerViewModel.BestPositions), Positions, nameof(EditableRatedPositionViewModel.Position));

        public CountryFilterViewModel CountryFilter { get; } = new(nameof(PlayerViewModel.Country));
    }
}

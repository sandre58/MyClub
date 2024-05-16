// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities.Comparaison;
using MyNet.Observable.Attributes;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Filters
{
    internal class RatedPositionFilterViewModel : IntegerFilterViewModel
    {
        [UpdateOnCultureChanged]
        public Position? Position { get; set; }

        public RatedPositionFilterViewModel(string propertyName) : base(propertyName, ComplexComparableOperator.EqualsTo, (int)PositionRating.Inefficient, (int)PositionRating.Natural)
        {
            Minimum = (int)PositionRating.Inefficient;
            Maximum = (int)PositionRating.Natural;
        }

        public RatedPositionFilterViewModel(string propertyName, ComplexComparableOperator comparaison, int from, int to) : base(
            propertyName, comparaison, from, to)
        { }

        protected override bool IsMatchProperty(object toCompare) => toCompare is RatedPositionViewModel playerPosition && playerPosition.Position.Equals(Position) && base.IsMatchProperty((int)playerPosition.Rating);

        protected override FilterViewModel CreateCloneInstance() => new RatedPositionFilterViewModel(PropertyName)
        {
            Position = Position,
            Operator = Operator,
            From = From,
            To = To,
            Minimum = Minimum,
            Maximum = Maximum,
            IsReadOnly = IsReadOnly
        };
    }
}

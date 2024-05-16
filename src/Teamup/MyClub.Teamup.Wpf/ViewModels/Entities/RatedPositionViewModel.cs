// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Wpf.Controls;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class RatedPositionViewModel(RatedPosition item, PlayerViewModel player) : EntityViewModelBase<RatedPosition>(item), IPositionWrapper
    {
        public PlayerViewModel Player { get; } = player;

        public Position Position => Item.Position;

        public PositionRating Rating => Item.Rating;

        public bool IsNatural => Item.IsNatural;

        public double OffsetX { get; set; }

        public double OffsetY { get; set; }
    }
}

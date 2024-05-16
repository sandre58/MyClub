// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.TacticAggregate;
using MyClub.Teamup.Wpf.Controls;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class TacticPositionViewModel(TacticPosition item) : EntityViewModelBase<TacticPosition>(item), IPositionWrapper
    {
        public Position Position => Item.Position;

        public int? Number => Item.Number;

        public double OffsetX
        {
            get => Item.OffsetX;
            set
            {
                // Noting
            }
        }

        public double OffsetY
        {
            get => Item.OffsetY;
            set
            {
                // Noting
            }
        }

        public ICollection<string> Instructions => Item.Instructions;
    }
}

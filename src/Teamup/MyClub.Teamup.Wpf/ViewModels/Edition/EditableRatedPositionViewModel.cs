// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.Controls;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableRatedPositionViewModel : EditableObject, IPositionWrapper
    {
        public Guid? Id { get; }

        [Display(Name = nameof(Position), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public Position Position { get; }

        [IsRequired]
        [Display(Name = nameof(Rating), ResourceType = typeof(MyClubResources))]
        public PositionRating Rating { get; set; } = PositionRating.VeryGood;

        [Display(Name = nameof(IsNatural), ResourceType = typeof(MyClubResources))]
        public bool IsNatural { get; set; }

        public double OffsetX { get; set; }

        public double OffsetY { get; set; }

        public EditableRatedPositionViewModel(Position position, Guid? id = null) => (Position, Id) = (position, id);

        internal void Reset()
        {
            IsNatural = false;
            OffsetX = 0;
            OffsetY = 0;
            Rating = PositionRating.VeryGood;
        }
    }
}

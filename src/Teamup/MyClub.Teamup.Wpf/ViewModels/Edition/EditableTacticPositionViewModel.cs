// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Edition;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Translatables;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.TacticAggregate;
using MyClub.Teamup.Wpf.Controls;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableTacticPositionViewModel : EditableObject, IPositionWrapper
    {
        public Guid? Id { get; }

        [Display(Name = nameof(Position), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public Position Position { get; }

        [Display(Name = nameof(Number), ResourceType = typeof(MyClubResources))]
        public AcceptableValue<int> Number { get; } = new AcceptableValue<int>(TacticPosition.AcceptableRangeNumber);

        [Display(Name = nameof(OffsetX), ResourceType = typeof(MyClubResources))]
        public double OffsetX { get; set; }

        [Display(Name = nameof(OffsetY), ResourceType = typeof(MyClubResources))]
        public double OffsetY { get; set; }

        public ICommand ResetOffsetCommand { get; }

        public StringListEditionViewModel InstructionsViewModel { get; } = new(MyClubResources.Instructions);

        public EditableTacticPositionViewModel(Position position, Guid? id = null)
        {
            (Position, Id) = (position, id);
            ResetOffsetCommand = CommandsManager.Create(ResetOffset, () => OffsetX != 0 || OffsetY != 0);
        }

        public void ResetOffset()
        {
            OffsetX = 0;
            OffsetY = 0;
        }

        public void Reset()
        {
            Number.Value = null;
            InstructionsViewModel.SetSource([]);
            ResetOffset();
        }
    }
}

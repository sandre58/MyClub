// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableRoundStageViewModel : EditableObject, IIdentifiable<Guid>
    {
        public EditableRoundStageViewModel(string name, string shortName, bool canRemove) : this(Guid.Empty, name, shortName, canRemove) { }

        public EditableRoundStageViewModel(Guid id, string name, string shortName, bool canRemove)
        {
            Id = id;
            Name = name;
            ShortName = shortName;
            CanRemove = canRemove;
        }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public Guid Id { get; }

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? Name { get; }

        [IsRequired]
        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        public string? ShortName { get; }

        public EditableDateTime CurrentDate { get; set; } = new();

        public EditableDateTime PostponedDateTime { get; set; } = new(false);

        public PostponedState PostponedState { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ScheduleAutomatic { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ScheduleStadiumsAutomatic { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanScheduleAutomatic { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanScheduleStadiumsAutomatic { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanRemove { get; }

        public ObservableCollection<EditableMatchViewModel> Matches { get; } = [];
    }
}

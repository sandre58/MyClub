// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using MyNet.Utilities;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyClub.CrossCutting.Localization;
using MyClub.Domain;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.UI.Collections;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableGroupViewModel(Guid? id = null) : EditableObject, ISimilar<EditableGroupViewModel>, IOrderable
    {
        public Guid? Id { get; } = id;

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string Name { get; set; } = string.Empty;

        [IsRequired]
        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        public string ShortName { get; set; } = string.Empty;

        public int Order { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public UiObservableCollection<TeamViewModel> Teams { get; } = [];

        public bool IsSimilar(EditableGroupViewModel? obj) => Name.ToLower().Equals(obj?.Name.ToLower());

        public override string ToString() => Name;
    }
}

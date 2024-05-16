// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Domain;
using MyNet.Observable;
using MyNet.Observable.Attributes;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableEmailViewModel : EditableObject
    {
        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        [UpdateOnCultureChanged]
        public virtual IList<string> Labels => MyClubResources.EmailLabels.Split(';');

        public string? Label { get; set; }

        public bool Default { get; set; }

        [Display(Name = nameof(Email), ResourceType = typeof(MyClubResources))]
        [IsEmailAddress(true)]
        public string Value { get; set; } = string.Empty;
    }
}

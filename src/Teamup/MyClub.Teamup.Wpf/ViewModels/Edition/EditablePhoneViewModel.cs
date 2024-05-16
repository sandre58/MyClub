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
    internal class EditablePhoneViewModel : EditableObject
    {
        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        [UpdateOnCultureChanged]
        public virtual IList<string> Labels => MyClubResources.PhoneLabels.Split(';');

        public string? Label { get; set; }

        public bool Default { get; set; }

        [Display(Name = nameof(Phone), ResourceType = typeof(MyClubResources))]
        [HasMaxLength(10)]
        [IsPhone(true)]
        public string Value { get; set; } = string.Empty;
    }
}

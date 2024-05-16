// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Forms;
using System.Windows.Media;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Observable.Attributes;
using MyClub.CrossCutting.Localization;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    public class RankLabelEditionViewModel : EditionViewModel
    {
        public RankLabelEditionViewModel()
        {
            ValidationRules.AddNotNull<RankLabelEditionViewModel, int>(x => x.FromRank, MyClubResources.FieldFromRankMustBeUpperOrEqualsThanToRankError, x => x <= ToRank);
            ValidationRules.AddNotNull<RankLabelEditionViewModel, int>(x => x.ToRank, MyClubResources.FieldFromRankMustBeUpperOrEqualsThanToRankError, x => FromRank <= x);
        }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool IsRange { get; set; }

        [Display(Name = nameof(FromRank), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public int FromRank { get; set; } = 1;

        [Display(Name = nameof(ToRank), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public int ToRank { get; set; } = 1;

        public Color? Color { get; set; }

        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Name { get; set; } = string.Empty;

        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string ShortName { get; set; } = string.Empty;

        [Display(Name = nameof(Description), ResourceType = typeof(MyClubResources))]
        public string? Description { get; set; }

        public void Load(EditableRankLabelViewModel item)
        {
            IsRange = item.FromRank != item.ToRank;
            Name = item.Name;
            ShortName = item.ShortName;
            FromRank = item.FromRank;
            ToRank = item.ToRank;
            Description = item.Description;
            Color = item.Color;
        }

        public void New()
        {
            IsRange = true;
            Name = MyClubResources.Rule.Increment([]);
            ShortName = MyClubResources.Rule.Substring(0, 3);
            FromRank = 1;
            ToRank = 2;
            Description = string.Empty;
            Color = Colors.Green;
        }

        protected override void SaveCore() { }

        protected virtual void OnIsRangeChanged()
        {
            if (!IsRange)
                ToRank = FromRank;
        }

        protected virtual void OnFromRankChanged()
        {
            if (!IsRange || FromRank.CompareTo(ToRank) > 0)
                ToRank = FromRank;
        }

        protected virtual void OnToRankChanged()
        {
            if (!IsRange || FromRank.CompareTo(ToRank) > 0)
                FromRank = ToRank;
        }
    }
}

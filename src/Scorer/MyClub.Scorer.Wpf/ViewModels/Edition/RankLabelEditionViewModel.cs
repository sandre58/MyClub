// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using MyClub.CrossCutting.Localization;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels.Edition;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class RankLabelEditionViewModel : EditionViewModel
    {
        public RankLabelEditionViewModel() { }

        public RankLabelEditionViewModel(EditableRankLabelViewModel item)
        {
            Name = item.Name;
            ShortName = item.ShortName;
            Description = item.Description;
            Color = item.Color;
        }

        public Color? Color { get; set; }

        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Name { get; set; } = string.Empty;

        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string ShortName { get; set; } = string.Empty;

        [Display(Name = nameof(Description), ResourceType = typeof(MyClubResources))]
        public string? Description { get; set; } = string.Empty;

        protected override void SaveCore() { }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using MyClub.CrossCutting.Localization;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableRankLabelViewModel : EditableObject
    {
        public EditableRankLabelViewModel(Interval<int> range) => Range = range;

        [IsRequired]
        [Display(Name = nameof(Range), ResourceType = typeof(MyClubResources))]
        public Interval<int> Range { get; set; }

        [Display(Name = nameof(Color), ResourceType = typeof(MyClubResources))]
        public Color? Color { get; set; }

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string Name { get; set; } = string.Empty;

        [IsRequired]
        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        public string ShortName { get; set; } = string.Empty;

        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        public string? Description { get; set; }
    }
}

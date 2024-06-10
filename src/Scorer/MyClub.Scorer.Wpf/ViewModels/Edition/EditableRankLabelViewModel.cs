// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows.Media;
using MyNet.Observable;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    public class EditableRankLabelViewModel : EditableObject
    {
        public EditableRankLabelViewModel(Interval<int> range) => Range = range;

        public Interval<int> Range { get; set; }

        public Color? Color { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ShortName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}

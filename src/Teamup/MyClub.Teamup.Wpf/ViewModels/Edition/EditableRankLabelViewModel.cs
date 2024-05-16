// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows.Media;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    public class EditableRankLabelViewModel : EditableObject
    {
        public int FromRank { get; set; }

        public int ToRank { get; set; }

        public Color? Color { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ShortName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [UpdateOnCultureChanged]
        [DependsOn(nameof(FromRank), nameof(ToRank))]
        public string DisplayRanks => FromRank == ToRank ? FromRank.ToString() : string.Join($" {UiResources.To} ", new[] { FromRank, ToRank });
    }
}

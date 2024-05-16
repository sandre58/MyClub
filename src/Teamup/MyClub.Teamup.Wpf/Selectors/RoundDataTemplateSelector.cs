// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Selectors
{
    internal class RoundDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? MatchdayTemplate { get; set; }

        public DataTemplate? KnockoutTemplate { get; set; }

        public DataTemplate? GroupStageTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
            => item switch
            {
                MatchdayViewModel => MatchdayTemplate,
                KnockoutViewModel => KnockoutTemplate,
                GroupStageViewModel => GroupStageTemplate,
                _ => (DataTemplate?)base.SelectTemplate(item, container)
            };
    }
}

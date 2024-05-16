// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Selectors
{
    internal class CompetitionDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? FriendlyTemplate { get; set; }

        public DataTemplate? LeagueTemplate { get; set; }

        public DataTemplate? CupTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
            => item switch
            {
                FriendlyViewModel => FriendlyTemplate,
                LeagueViewModel => LeagueTemplate,
                CupViewModel => CupTemplate,
                _ => (DataTemplate?)base.SelectTemplate(item, container)
            };
    }
}

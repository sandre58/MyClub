// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Scorer.Wpf.Selectors
{
    internal class TeamDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
            => item switch
            {
                EditableTeamViewModel => TeamTemplate,
                TeamViewModel => TeamTemplate,
                IVirtualTeamViewModel => VirtualTeamTemplate,
                _ => base.SelectTemplate(item, container),
            };

        public DataTemplate? TeamTemplate { get; set; }

        public DataTemplate? VirtualTeamTemplate { get; set; }
    }
}

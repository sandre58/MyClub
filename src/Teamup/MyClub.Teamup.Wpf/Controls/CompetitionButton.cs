// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Controls
{
    internal class CompetitionButton : ItemButton<CompetitionViewModel>
    {
        static CompetitionButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(CompetitionButton), new FrameworkPropertyMetadata(typeof(CompetitionButton)));

        public CompetitionButton() { }
    }
}

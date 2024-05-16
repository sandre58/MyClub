// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.Controls
{
    internal class MatchdayButton : ItemButton<MatchdayViewModel>
    {
        static MatchdayButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(MatchdayButton), new FrameworkPropertyMetadata(typeof(MatchdayButton)));

        public MatchdayButton() { }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.Controls
{
    internal class StadiumButton : ItemButton<StadiumViewModel>
    {
        static StadiumButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(StadiumButton), new FrameworkPropertyMetadata(typeof(StadiumButton)));

        public StadiumButton() { }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Controls
{
    internal class TrainingSessionButton : ItemButton<TrainingSessionViewModel>
    {
        static TrainingSessionButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(TrainingSessionButton), new FrameworkPropertyMetadata(typeof(TrainingSessionButton)));

        public TrainingSessionButton() { }
    }
}

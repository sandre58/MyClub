// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Controls
{
    internal class PlayerButton : ItemButton<PlayerViewModel>
    {
        static PlayerButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(PlayerButton), new FrameworkPropertyMetadata(typeof(PlayerButton)));

        public PlayerButton() { }
    }
}

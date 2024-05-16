// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Controls
{
    internal class KnockoutButton : ItemButton<KnockoutViewModel>
    {
        static KnockoutButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(KnockoutButton), new FrameworkPropertyMetadata(typeof(KnockoutButton)));

        public KnockoutButton() { }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Input;

namespace MyClub.Teamup.Wpf.Views.CompetitionPage
{
    /// <summary>
    /// Interaction logic for TrainingsView.xaml
    /// </summary>
    public partial class MatchdayToolBar
    {
        public MatchdayToolBar() => InitializeComponent();

        public static readonly DependencyProperty RemoveCommandProperty
            = DependencyProperty.RegisterAttached(nameof(RemoveCommand), typeof(ICommand), typeof(MatchdayToolBar), new PropertyMetadata(null));

        public ICommand RemoveCommand
        {
            get => (ICommand)GetValue(RemoveCommandProperty);
            set => SetValue(RemoveCommandProperty, value);
        }
    }
}

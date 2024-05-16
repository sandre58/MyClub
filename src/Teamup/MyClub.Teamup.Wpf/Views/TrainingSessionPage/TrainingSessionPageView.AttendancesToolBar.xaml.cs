// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Input;

namespace MyClub.Teamup.Wpf.Views.TrainingSessionPage
{
    /// <summary>
    /// Interaction logic for PlayersView.xaml
    /// </summary>
    public partial class TrainingSessionPageAttendancesPlayerToolBar
    {
        public TrainingSessionPageAttendancesPlayerToolBar() => InitializeComponent();

        public static readonly DependencyProperty RemoveCommandProperty
            = DependencyProperty.RegisterAttached(nameof(RemoveCommand), typeof(ICommand), typeof(TrainingSessionPageAttendancesPlayerToolBar), new PropertyMetadata(null));

        public ICommand RemoveCommand
        {
            get => (ICommand)GetValue(RemoveCommandProperty);
            set => SetValue(RemoveCommandProperty, value);
        }
    }
}

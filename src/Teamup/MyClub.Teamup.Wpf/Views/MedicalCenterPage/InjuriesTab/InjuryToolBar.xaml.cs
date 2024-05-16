// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Input;

namespace MyClub.Teamup.Wpf.Views.MedicalCenterPage.InjuriesTab
{
    /// <summary>
    /// Interaction logic for PlayersView.xaml
    /// </summary>
    public partial class InjuryToolBar
    {
        public InjuryToolBar() => InitializeComponent();

        public static readonly DependencyProperty RemoveCommandProperty
            = DependencyProperty.RegisterAttached(nameof(RemoveCommand), typeof(ICommand), typeof(InjuryToolBar), new PropertyMetadata(null));

        public ICommand RemoveCommand
        {
            get => (ICommand)GetValue(RemoveCommandProperty);
            set => SetValue(RemoveCommandProperty, value);
        }

        public static readonly DependencyProperty EditCommandProperty
    = DependencyProperty.RegisterAttached(nameof(EditCommand), typeof(ICommand), typeof(InjuryToolBar), new PropertyMetadata(null));

        public ICommand EditCommand
        {
            get => (ICommand)GetValue(EditCommandProperty);
            set => SetValue(EditCommandProperty, value);
        }
    }
}

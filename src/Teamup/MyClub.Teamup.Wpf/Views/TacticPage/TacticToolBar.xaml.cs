// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Input;

namespace MyClub.Teamup.Wpf.Views.TacticPage
{
    /// <summary>
    /// Interaction logic for PlayersView.xaml
    /// </summary>
    public partial class TacticToolBar
    {
        public TacticToolBar() => InitializeComponent();

        public static readonly DependencyProperty RemoveCommandProperty
            = DependencyProperty.RegisterAttached(nameof(RemoveCommand), typeof(ICommand), typeof(TacticToolBar), new PropertyMetadata(null));

        public ICommand RemoveCommand
        {
            get => (ICommand)GetValue(RemoveCommandProperty);
            set => SetValue(RemoveCommandProperty, value);
        }

        public static readonly DependencyProperty EditCommandProperty
            = DependencyProperty.RegisterAttached(nameof(EditCommand), typeof(ICommand), typeof(TacticToolBar), new PropertyMetadata(null));

        public ICommand EditCommand
        {
            get => (ICommand)GetValue(EditCommandProperty);
            set => SetValue(EditCommandProperty, value);
        }
    }
}

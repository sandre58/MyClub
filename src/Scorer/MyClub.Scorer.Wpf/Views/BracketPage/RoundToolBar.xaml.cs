// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Input;

namespace MyClub.Scorer.Wpf.Views.BracketPage
{
    /// <summary>
    /// Interaction logic for TrainingsView.xaml
    /// </summary>
    public partial class RoundToolBar
    {
        public RoundToolBar() => InitializeComponent();

        public static readonly DependencyProperty RemoveCommandProperty
            = DependencyProperty.RegisterAttached(nameof(RemoveCommand), typeof(ICommand), typeof(RoundToolBar), new PropertyMetadata(null));

        public ICommand RemoveCommand
        {
            get => (ICommand)GetValue(RemoveCommandProperty);
            set => SetValue(RemoveCommandProperty, value);
        }
    }
}

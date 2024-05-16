// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Wpf.Controls
{
    internal class InjurySelector : ListBox
    {
        static InjurySelector() => DefaultStyleKeyProperty.OverrideMetadata(typeof(InjurySelector), new FrameworkPropertyMetadata(typeof(InjurySelector)));

        #region IsReadOnly

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(InjurySelector), new PropertyMetadata(false));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        #endregion

        #region IsFemale

        public static readonly DependencyProperty IsFemaleProperty = DependencyProperty.Register(nameof(IsFemale), typeof(bool), typeof(InjurySelector), new PropertyMetadata(false));

        public bool IsFemale
        {
            get => (bool)GetValue(IsFemaleProperty);
            set => SetValue(IsFemaleProperty, value);
        }

        #endregion

        #region Severity

        public static readonly DependencyProperty SeverityProperty = DependencyProperty.Register(nameof(Severity), typeof(InjurySeverity?), typeof(InjurySelector), new PropertyMetadata(null));

        public InjurySeverity? Severity
        {
            get => (InjurySeverity?)GetValue(SeverityProperty);
            set => SetValue(SeverityProperty, value);
        }

        #endregion
    }
}

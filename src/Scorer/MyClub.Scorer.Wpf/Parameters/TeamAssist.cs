// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.Parameters
{
    internal static class TeamAssist
    {

        public static readonly DependencyProperty FixtureStateProperty = DependencyProperty.RegisterAttached(
            "FixtureState", typeof(QualificationState), typeof(TeamAssist), new PropertyMetadata(QualificationState.Unknown));

        public static void SetFixtureState(FrameworkElement target, QualificationState value) => target.SetValue(FixtureStateProperty, value);

        public static QualificationState GetFixtureState(FrameworkElement target) => (QualificationState)target.GetValue(FixtureStateProperty);

    }
}

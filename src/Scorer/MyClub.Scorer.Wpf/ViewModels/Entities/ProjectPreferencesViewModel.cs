// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Reactive.Linq;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Observable;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class ProjectPreferencesViewModel : ObservableObject
    {
        private ProjectPreferences? _currentPreferences;
        private IDisposable? _disposable;

        public bool IsLoaded => _currentPreferences is not null;

        public bool TreatNoStadiumAsWarning => _currentPreferences?.TreatNoStadiumAsWarning ?? true;

        public TimeSpan PeriodForPreviousMatches => (_currentPreferences?.PeriodForPreviousMatches).GetValueOrDefault();

        public TimeSpan PeriodForNextMatches => (_currentPreferences?.PeriodForNextMatches).GetValueOrDefault();

        public bool ShowNextMatchFallback => _currentPreferences?.ShowNextMatchFallback ?? true;

        public bool ShowLastMatchFallback => _currentPreferences?.ShowLastMatchFallback ?? true;

        public void Clear()
        {
            _disposable?.Dispose();
            _currentPreferences = null;
        }

        public void Load(ProjectPreferences preferences)
        {
            _currentPreferences = preferences;
            _disposable = Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(x => preferences.PropertyChanged += x, x => preferences.PropertyChanged -= x).Subscribe(x => RaisePropertyChanged(x.EventArgs.PropertyName));
        }

        protected override void Cleanup()
        {
            _disposable?.Dispose();
            base.Cleanup();
        }
    }
}

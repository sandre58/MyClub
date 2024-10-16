// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Utilities;
using MyNet.Utilities.Localization;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class MatchesProvider : EntitiesProviderBase<MatchViewModel>
    {
        private readonly CompetitionInfoProvider _competitionInfoProvider;
        private readonly IDisposable _disposable;

        public MatchesProvider(ProjectInfoProvider projectInfoProvider, CompetitionInfoProvider competitionInfoProvider) : base(projectInfoProvider)
        {
            _competitionInfoProvider = competitionInfoProvider;

            _disposable = Connect().Throttle(80.Milliseconds()).Subscribe(_ => UpdateDisplayNames());

            GlobalizationService.Current.CultureChanged += OnCultureChanged;
        }

        protected override IObservable<IChangeSet<MatchViewModel>> ProvideObservable(IProject project) => _competitionInfoProvider.GetCompetition().Matches.ToObservableChangeSet();

        private void OnCultureChanged(object? sender, EventArgs e) => UpdateDisplayNames();

        private void UpdateDisplayNames() => Items.OrderBy(x => x.Date).ForEach((x, y) =>
        {
            x.DisplayName = (y + 1).ToString(MyClubResources.MatchX);
            x.DisplayShortName = (y + 1).ToString(MyClubResources.MatchXAbbr);
        });

        protected override void Cleanup()
        {
            GlobalizationService.Current.CultureChanged -= OnCultureChanged;
            _disposable.Dispose();
            base.Cleanup();
        }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.Notifications;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.Services.Handlers
{
    internal sealed class NoneVenueValidationHandler : ProjectNotificationHandler
    {
        private const string Category = "NoneVenue";

        private readonly CompositeDisposable _disposable;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly MatchesProvider _matchesProvider;

        public NoneVenueValidationHandler(ProjectInfoProvider projectInfoProvider,
                                          MatchesProvider matchesProvider,
                                          MatchPresentationService matchPresentationService) : base(x => x.Category.StartsWith(Category))
        {
            _matchesProvider = matchesProvider;
            _matchPresentationService = matchPresentationService;
            _disposable = new(matchesProvider.Connect().WhenPropertyChanged(x => x.Stadium).Subscribe(x => projectInfoProvider.TreatNoStadiumAsWarning.IfTrue(() => CheckMatch(x.Sender))),
                                                  projectInfoProvider.WhenPropertyChanged(x => x.TreatNoStadiumAsWarning).Subscribe(_ =>
                                                  {
                                                      if (projectInfoProvider.TreatNoStadiumAsWarning)
                                                          CheckAllMatches();
                                                      else
                                                          Unnotify(x => x.Category.StartsWith(Category));
                                                  }));
        }

        private void CheckAllMatches() => _matchesProvider.Items.ForEach(CheckMatch);

        private void CheckMatch(MatchViewModel match)
        {
            var category = $"{Category}-{match.Id}";
            if (match.Stadium is not null)
            {
                Unnotify(x => x.Category == category);
            }
            else
            {
                var notification = new ActionNotification(MyClubResources.NoStadiumWarning.FormatWith($"{match.HomeTeam.Name} - {match.AwayTeam.Name} ({match.Parent.Name})"), MyClubResources.NoStadiumWarningTitle, NotificationSeverity.Warning, category, false, async x => await _matchPresentationService.EditAsync(match).ConfigureAwait(false));
                Notify(notification);
            }
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            _disposable.Dispose();
        }
    }
}

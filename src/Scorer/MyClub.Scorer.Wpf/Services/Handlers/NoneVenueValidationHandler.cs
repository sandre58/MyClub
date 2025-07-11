// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.Notifications;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.Services.Handlers
{
    internal sealed class NoneVenueValidationHandler : ProjectNotificationHandler
    {
        private const string Category = "NoneVenue";

        private readonly CompositeDisposable _disposable;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly MatchService _matchService;
        private readonly ProjectInfoProvider _projectInfoProvider;
        private readonly MatchesProvider _matchesProvider;

        public NoneVenueValidationHandler(ProjectInfoProvider projectInfoProvider,
                                          MatchesProvider matchesProvider,
                                          MatchPresentationService matchPresentationService,
                                          MatchService matchService) : base(x => x.Category.StartsWith(Category))
        {
            _matchService = matchService;
            _matchesProvider = matchesProvider;
            _projectInfoProvider = projectInfoProvider;
            _matchPresentationService = matchPresentationService;
            _disposable = new(matchesProvider.Connect().WhenPropertyChanged(x => x.Stadium).Subscribe(x => projectInfoProvider.Preferences.TreatNoStadiumAsWarning.IfTrue(() => CheckMatch(x.Sender))),
                                                  projectInfoProvider.WhenPropertyChanged(x => x.Preferences.TreatNoStadiumAsWarning, false).Subscribe(_ =>
                                                  {
                                                      if (projectInfoProvider.Preferences.TreatNoStadiumAsWarning)
                                                          CheckAllMatches();
                                                      else
                                                          Unnotify(x => x.Category.StartsWith(Category));
                                                  }));
        }

        private void CheckAllMatches()
        {
            Unnotify(x => x.Category.StartsWith(Category));

            if (_projectInfoProvider.Preferences.TreatNoStadiumAsWarning)
            {
                var matchIds = _matchService.GetMatchIdsWithInvalidVenues();

                matchIds.Select(_matchesProvider.GetOrThrow).ToList().ForEach(x => UpdateNotification(x, false));
            }
        }

        private void CheckMatch(MatchViewModel match) => UpdateNotification(match, !_projectInfoProvider.Preferences.TreatNoStadiumAsWarning || _matchService.VenueIsValid(match.Id));

        private void UpdateNotification(MatchViewModel match, bool isValid)
        {
            var category = $"{Category}-{match.Id}";
            if (isValid)
            {
                Unnotify(x => x.Category == category);
            }
            else
            {
                var notification = new ActionNotification(MyClubResources.NoStadiumWarning.FormatWith($"{match.Home.Team.Name} - {match.Away.Team.Name} ({(match.Parent.Stage is not ICompetitionViewModel ? $"{match.Parent.Stage.Name} - {match.Parent.Name}" : match.Parent.Name)})"), MyClubResources.NoStadiumWarningTitle, NotificationSeverity.Warning, category, false, async x => await _matchPresentationService.EditAsync(match).ConfigureAwait(false));
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

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class MatchesEditionViewModel : EditionViewModel
    {
        private readonly MatchService _matchService;
        private readonly StadiumPresentationService _stadiumPresentationService;
        private readonly StadiumsProvider _stadiumsProvider;

        public ObservableCollection<MatchEditionViewModel> Matches { get; private set; } = [];

        public MatchesEditionViewModel(MatchService matchService, StadiumPresentationService stadiumPresentationService, StadiumsProvider stadiumsProvider)
        {
            _matchService = matchService;
            _stadiumsProvider = stadiumsProvider;
            _stadiumPresentationService = stadiumPresentationService;
            Mode = ScreenMode.Edition;
        }

        public void Load(IEnumerable<MatchViewModel> matches)
        {
            Matches.Clear();
            foreach (var match in matches)
                Matches.Add(CreateEditableMatch(match));
        }

        private MatchEditionViewModel CreateEditableMatch(MatchViewModel match)
        {
            var result = new MatchEditionViewModel(_matchService, _stadiumPresentationService, _stadiumsProvider);

            result.Load(match.Parent, match.Id);
            result.Refresh();

            return result;
        }

        #region Validate

        protected override void SaveCore()
            => _matchService.Save(Matches.Select(x => new MatchDto
            {
                Id = x.ItemId,
                ParentId = x.Parent?.Id,
                Date = x.Date.GetValueOrDefault().ToUtcDateTime(x.Time),
                NeutralVenue = x.NeutralVenue,
                Stadium = x.StadiumSelection.SelectedItem is not null ? new StadiumDto
                {
                    Id = x.StadiumSelection.SelectedItem.Id,
                    Name = x.StadiumSelection.SelectedItem.Name,
                    Ground = x.StadiumSelection.SelectedItem.Ground,
                    Address = x.StadiumSelection.SelectedItem?.Address,
                } : null,
                HomeScore = x.HomeScore.Value,
                AwayScore = x.AwayScore.Value,
                HomeShootoutScore = x.HomeShootoutScore.Value,
                AwayShootoutScore = x.AwayShootoutScore.Value,
                HomeIsWithdrawn = x.HomeIsWithdrawn,
                AwayIsWithdrawn = x.AwayIsWithdrawn,
                AfterExtraTime = x.AfterExtraTime,
                HomePenaltyPoints = x.HomePenaltyPoints,
                AwayPenaltyPoints = x.AwayPenaltyPoints,
                State = x.State,
                PostponedDate = x.ShowPostponedDate ? x.PostponedDate?.ToUtcDateTime(x.PostponedTime ?? (x.Parent?.GetDefaultDateTime().TimeOfDay).GetValueOrDefault()) : null,
            }).ToList(), false);

        #endregion

        protected override void Cleanup()
        {
            base.Cleanup();
            Matches.ForEach(x => x.Dispose());
        }
    }
}

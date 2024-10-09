// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using DynamicData;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.ViewModels.Edition;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class RankingRulesEditionViewModel : EditionViewModel
    {
        private readonly LeagueService _leagueService;

        public RankingRulesEditionViewModel(LeagueService leagueService, TeamsProvider teamsProvider)
        {
            _leagueService = leagueService;
            RankingRules = new(new ObservableSourceProvider<IEditableTeamViewModel>(teamsProvider.Connect().Transform(x => (IEditableTeamViewModel)x)));
        }

        public EditableRankingRulesViewModel RankingRules { get; }

        protected override void RefreshCore()
        {
            var rankingRules = _leagueService.GetRankingRules();

            if (rankingRules.Rules is not null)
                RankingRules.Load(rankingRules.Rules, rankingRules.Labels ?? [], rankingRules.PenaltyPoints ?? [], _leagueService.GetMatchFormat().ShootoutIsEnabled);
        }

        protected override void SaveCore()
            => _leagueService.UpdateRankingRules(new RankingRulesDto
            {
                Rules = RankingRules.Create(),
                PenaltyPoints = RankingRules.CreatePenaltyPoints(),
                Labels = RankingRules.CreateLabels(),
            });
    }
}

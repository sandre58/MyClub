// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections;
using MyNet.Observable.Translatables;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;
using MyNet.Wpf.Extensions;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class RankingColumnComputerWrapper : DisplayWrapper<IRankingColumnComputer>
    {
        public RankingColumnComputerWrapper(IRankingColumnComputer item, string resourceKey) : base(item, resourceKey) => Key = resourceKey;

        public string Key { get; }

        public bool IsEnabled { get; set; } = true;

        public bool IsActive { get; set; }
    }

    internal class RankingRulesEditionViewModel : EditionViewModel
    {
        private static List<DisplayWrapper<RankingRowComparer>> GetAvailableRankingRowComparers() =>
            [
                new DisplayWrapper<RankingRowComparer>(RankingComparer.AllAvailableComparers[nameof(RankingRowByPointsComparer)], nameof(MyClubResources.SortingByPoints)),
                new DisplayWrapper<RankingRowComparer>(RankingComparer.AllAvailableComparers[nameof(RankingRowByResultsBetweenTeamsComparer)], nameof(MyClubResources.SortingByResultsBetweenTeams)),
                new DisplayWrapper<RankingRowComparer>(RankingComparer.AllAvailableComparers[nameof(RankingRowByGoalsDifferenceComparer)], nameof(MyClubResources.SortingByGoalsDifference)),
                new DisplayWrapper<RankingRowComparer>(RankingComparer.AllAvailableComparers[nameof(RankingRowByGoalsForComparer)], nameof(MyClubResources.SortingByGoalsFor)),
                new DisplayWrapper<RankingRowComparer>(RankingComparer.AllAvailableComparers[nameof(RankingRowByGoalsAgainstComparer)], nameof(MyClubResources.SortingByGoalsAgainst)),
                new DisplayWrapper<RankingRowComparer>(RankingComparer.AllAvailableComparers[nameof(RankingRowByGamesWonComparer)], nameof(MyClubResources.SortingByGamesWon)),
                new DisplayWrapper<RankingRowComparer>(RankingComparer.AllAvailableComparers[nameof(RankingRowByGamesLostComparer)], nameof(MyClubResources.SortingByGamesLost)),
                new DisplayWrapper<RankingRowComparer>(RankingComparer.AllAvailableComparers[nameof(RankingRowByGamesWithdrawnComparer)], nameof(MyClubResources.SortingByGamesWithdrawn)),
            ];

        private readonly LeaguePresentationService _leaguePresentationService;
        private readonly LeagueService _leagueService;
        private readonly ThreadSafeObservableCollection<TeamViewModel> _teams = [];

        public RankingRulesEditionViewModel(LeagueService leagueService, LeaguePresentationService leaguePresentationService, TeamsProvider teamsProvider)
        {
            _leaguePresentationService = leaguePresentationService;
            _leagueService = leagueService;
            Teams = new(_teams);

            AddTeamPenaltyCommand = CommandsManager.CreateNotNull<TeamViewModel>(x => Penalties.Add(new EditableTeamPenaltyViewModel(x)), x => !Penalties.Select(x => x.Team).Contains(x));
            RemoveTeamPenaltyCommand = CommandsManager.CreateNotNull<EditableTeamPenaltyViewModel>(x => Penalties.Remove(x), x => x is not null);
            RemoveRankLabelCommand = CommandsManager.CreateNotNull<EditableRankLabelViewModel>(x => RankingLabels.Remove(x), x => x is not null);
            EditRankLabelCommand = CommandsManager.CreateNotNull<EditableRankLabelViewModel>(async x => await EditRankLabelAsync(x).ConfigureAwait(false), x => x is not null);
            AddRankLabelCommand = CommandsManager.Create(async () => await AddRankLabelAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                teamsProvider.ConnectById().AutoRefresh(x => x.Name).SortBy(x => x.Name).Bind(_teams).Subscribe()
            ]);
        }

        [Display(Name = nameof(PointsByGamesWon), ResourceType = typeof(MyClubResources))]
        public int? PointsByGamesWon { get; set; }

        [Display(Name = nameof(PointsByGamesDrawn), ResourceType = typeof(MyClubResources))]
        public int? PointsByGamesDrawn { get; set; }

        [Display(Name = nameof(PointsByGamesLost), ResourceType = typeof(MyClubResources))]
        public int? PointsByGamesLost { get; set; }

        [Display(Name = nameof(PointsByGamesWithdrawn), ResourceType = typeof(MyClubResources))]
        public int? PointsByGamesWithdrawn { get; set; }

        [Display(Name = nameof(PointsByGamesWonAfterShootouts), ResourceType = typeof(MyClubResources))]
        public int? PointsByGamesWonAfterShootouts { get; set; }

        [Display(Name = nameof(PointsByGamesLostAfterShootouts), ResourceType = typeof(MyClubResources))]
        public int? PointsByGamesLostAfterShootouts { get; set; }

        [HasAnyItems]
        public ObservableCollection<DisplayWrapper<RankingRowComparer>> RankingRowComparers { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ObservableCollection<DisplayWrapper<RankingRowComparer>> UnusedRankingRowComparers { get; } = [];

        public ObservableCollection<RankingColumnComputerWrapper> RankingColumnComputers { get; } = [
            new RankingColumnComputerWrapper(new PlayedColumnComputer(), nameof(MyClubResources.GamesPlayed)),
            new RankingColumnComputerWrapper(new GamesWonColumnComputer(), nameof(MyClubResources.GamesWon)),
            new RankingColumnComputerWrapper(new GamesDrawnColumnComputer(), nameof(MyClubResources.GamesDrawn)),
            new RankingColumnComputerWrapper(new GamesLostColumnComputer(), nameof(MyClubResources.GamesLost)),
            new RankingColumnComputerWrapper(new GamesWithdrawnColumnComputer(), nameof(MyClubResources.GamesWithdrawn)),
            new RankingColumnComputerWrapper(new GoalsForColumnComputer(), nameof(MyClubResources.GoalsFor)),
            new RankingColumnComputerWrapper(new GoalsAgainstColumnComputer(), nameof(MyClubResources.GoalsAgainst)),
            new RankingColumnComputerWrapper(new GoalsDifferenceColumnComputer(), nameof(MyClubResources.GoalsDifference)),
            new RankingColumnComputerWrapper(new DefaultRankingColumnComputer<int>((matches, team) => matches.Count(x => x.GetDetailledResultOf(team) == MatchResultDetailled.WonAfterShootouts)), nameof(MyClubResources.GamesWonAfterShootouts)),
            new RankingColumnComputerWrapper(new DefaultRankingColumnComputer<int>((matches, team) => matches.Count(x => x.GetDetailledResultOf(team) == MatchResultDetailled.LostAfterShootouts)), nameof(MyClubResources.GamesLostAfterShootouts)),
        ];

        [HasUniqueItems]
        [Display(Name = nameof(Penalties), ResourceType = typeof(MyClubResources))]
        public ThreadSafeObservableCollection<EditableTeamPenaltyViewModel> Penalties { get; } = [];

        [HasUniqueItems]
        [Display(Name = nameof(RankingLabels), ResourceType = typeof(MyClubResources))]
        public ThreadSafeObservableCollection<EditableRankLabelViewModel> RankingLabels { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<TeamViewModel> Teams { get; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ShowShootouts { get; private set; }

        public ICommand AddTeamPenaltyCommand { get; private set; }

        public ICommand RemoveTeamPenaltyCommand { get; private set; }

        public ICommand AddRankLabelCommand { get; private set; }

        public ICommand EditRankLabelCommand { get; private set; }

        public ICommand RemoveRankLabelCommand { get; private set; }

        protected override void RefreshCore()
        {
            var rankingRules = _leagueService.GetRankingRules();
            ShowShootouts = _leagueService.GetMatchFormat().ShootoutIsEnabled;

            PointsByGamesWon = rankingRules.Rules is not null ? GetPoints(rankingRules.Rules, MatchResultDetailled.Won) : null;
            PointsByGamesWonAfterShootouts = rankingRules.Rules is not null ? GetPoints(rankingRules.Rules, MatchResultDetailled.WonAfterShootouts) : null;
            PointsByGamesDrawn = rankingRules.Rules is not null ? GetPoints(rankingRules.Rules, MatchResultDetailled.Drawn) : null;
            PointsByGamesLost = rankingRules.Rules is not null ? GetPoints(rankingRules.Rules, MatchResultDetailled.Lost) : null;
            PointsByGamesLostAfterShootouts = rankingRules.Rules is not null ? GetPoints(rankingRules.Rules, MatchResultDetailled.LostAfterShootouts) : null;
            PointsByGamesWithdrawn = rankingRules.Rules is not null ? GetPoints(rankingRules.Rules, MatchResultDetailled.Withdrawn) : null;

            var availableRankingRowComparers = GetAvailableRankingRowComparers();

            RankingRowComparers.Set(rankingRules.Rules?.Comparer.Select(x => availableRankingRowComparers.Find(y => y.Item.GetType() == x.GetType())).NotNull());
            UnusedRankingRowComparers.Set(availableRankingRowComparers.Except(RankingRowComparers));

            RankingColumnComputers.ForEach(x =>
            {
                x.IsActive = rankingRules.Rules?.Computers.ContainsKey(x.Key) ?? false;
                x.IsEnabled = ShowShootouts || x.Key != nameof(MyClubResources.GamesWonAfterShootouts) && x.Key != nameof(MyClubResources.GamesLostAfterShootouts);
            });

            RankingLabels.Set(rankingRules.Labels?.Select(x => new EditableRankLabelViewModel
            {
                Color = x.Value.Color?.ToColor(),
                Description = x.Value.Description,
                FromRank = x.Key.Min ?? 1,
                ToRank = x.Key.Max ?? 1,
                Name = x.Value.Name,
                ShortName = x.Value.ShortName,
            }));

            Penalties.Set(rankingRules.PenaltyPoints?.Select(x => new EditableTeamPenaltyViewModel(_teams.GetById(x.Key))
            {
                Points = x.Value
            }));
        }

        protected override void SaveCore()
        {
            var pointsByResult = new Dictionary<MatchResultDetailled, int>();

            if (PointsByGamesWon.HasValue)
                pointsByResult.Add(MatchResultDetailled.Won, PointsByGamesWon.Value);

            if (PointsByGamesWonAfterShootouts.HasValue)
                pointsByResult.Add(MatchResultDetailled.WonAfterShootouts, PointsByGamesWonAfterShootouts.Value);

            if (PointsByGamesDrawn.HasValue)
                pointsByResult.Add(MatchResultDetailled.Drawn, PointsByGamesDrawn.Value);

            if (PointsByGamesLost.HasValue)
                pointsByResult.Add(MatchResultDetailled.Lost, PointsByGamesLost.Value);

            if (PointsByGamesLostAfterShootouts.HasValue)
                pointsByResult.Add(MatchResultDetailled.LostAfterShootouts, PointsByGamesLostAfterShootouts.Value);

            if (PointsByGamesWithdrawn.HasValue)
                pointsByResult.Add(MatchResultDetailled.Withdrawn, PointsByGamesWithdrawn.Value);

            _leagueService.UpdateRankingRules(new RankingRulesDto
            {
                Rules = new RankingRules(pointsByResult, new RankingComparer(RankingRowComparers.Select(x => x.Item)), RankingColumnComputers.Where(x => x.IsEnabled && x.IsActive).ToDictionary(x => x.Key, x => x.Item)),
                PenaltyPoints = Penalties.ToDictionary(x => x.Team.Id, x => x.Points),
                Labels = RankingLabels.ToDictionary(x => new AcceptableValueRange<int>(x.FromRank, x.ToRank), x => new RankLabel(x.Color?.ToString(), x.Name, x.ShortName, x.Description)),
            });
        }

        private static int? GetPoints(RankingRules rankingRules, MatchResultDetailled result) => rankingRules.PointsNumberByResult.ContainsKey(result) ? rankingRules.GetPoints(result) : null;

        private async Task AddRankLabelAsync()
        {
            var item = await _leaguePresentationService.CreateRankLabelAsync().ConfigureAwait(false);

            if (item is not null)
                RankingLabels.Add(item);
        }

        private async Task EditRankLabelAsync(EditableRankLabelViewModel oldItem) => await _leaguePresentationService.UpdateRankLabelAsync(oldItem).ConfigureAwait(false);
    }
}

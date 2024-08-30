// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.Observable.Translatables;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableRankingRulesViewModel : NavigableWorkspaceViewModel
    {
        private static List<RankingRowComparerWrapper> GetAvailableRankingRowComparers() =>
            [
                new RankingRowComparerWrapper(RankingComparer.AllAvailableComparers[nameof(RankingRowByPointsComparer)], nameof(MyClubResources.SortingByPoints)),
                new RankingRowComparerWrapper(RankingComparer.AllAvailableComparers[nameof(RankingRowByResultsBetweenTeamsComparer)], nameof(MyClubResources.SortingByResultsBetweenTeams)),
                new RankingRowComparerWrapper(RankingComparer.AllAvailableComparers[nameof(RankingRowByGoalsDifferenceComparer)], nameof(MyClubResources.SortingByGoalsDifference)),
                new RankingRowComparerWrapper(RankingComparer.AllAvailableComparers[nameof(RankingRowByGoalsForComparer)], nameof(MyClubResources.SortingByGoalsFor)),
                new RankingRowComparerWrapper(RankingComparer.AllAvailableComparers[nameof(RankingRowByGoalsAgainstComparer)], nameof(MyClubResources.SortingByGoalsAgainst)),
                new RankingRowComparerWrapper(RankingComparer.AllAvailableComparers[nameof(RankingRowByGamesWonComparer)], nameof(MyClubResources.SortingByGamesWon)),
                new RankingRowComparerWrapper(RankingComparer.AllAvailableComparers[nameof(RankingRowByGamesWonAfterShootoutsComparer)], nameof(MyClubResources.SortingByGamesWonAfterShootouts)),
                new RankingRowComparerWrapper(RankingComparer.AllAvailableComparers[nameof(RankingRowByGamesLostComparer)], nameof(MyClubResources.SortingByGamesLost)),
                new RankingRowComparerWrapper(RankingComparer.AllAvailableComparers[nameof(RankingRowByGamesLostAfterShootoutsComparer)], nameof(MyClubResources.SortingByGamesLostAfterShootouts)),
                new RankingRowComparerWrapper(RankingComparer.AllAvailableComparers[nameof(RankingRowByGamesWithdrawnComparer)], nameof(MyClubResources.SortingByGamesWithdrawn)),
            ];

        private readonly UiObservableCollection<ITeamViewModel> _teams = [];

        public EditableRankingRulesViewModel(ISourceProvider<ITeamViewModel> teamsProvider)
        {
            Teams = new(_teams);

            AddTeamPenaltyCommand = CommandsManager.CreateNotNull<ITeamViewModel>(x => Penalties.Add(new EditableTeamPenaltyViewModel(x)), x => !Penalties.Select(x => x.Team).Contains(x));
            RemoveTeamPenaltyCommand = CommandsManager.CreateNotNull<EditableTeamPenaltyViewModel>(x => Penalties.Remove(x), x => x is not null);

            Disposables.AddRange(
            [
                teamsProvider.Connect().AutoRefresh(x => x.Name).Sort(SortExpressionComparer<ITeamViewModel>.Ascending(x => x.Name)).Bind(_teams).Subscribe(),
                teamsProvider.Connect().Subscribe(_ => Labels.Update(teamsProvider.Source.Count)),
            ]);

            Reset();
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
        public UiObservableCollection<RankingRowComparerWrapper> RankingRowComparers { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public UiObservableCollection<RankingRowComparerWrapper> UnusedRankingRowComparers { get; } = [];

        public UiObservableCollection<RankingColumnComputerWrapper> RankingColumnComputers { get; } = [
            new RankingColumnComputerWrapper(new PlayedColumnComputer(), nameof(MyClubResources.GamesPlayed)),
            new RankingColumnComputerWrapper(new GamesWonColumnComputer(), nameof(MyClubResources.GamesWon)),
            new RankingColumnComputerWrapper(new GamesWonAfterShootoutsColumnComputer(), nameof(MyClubResources.GamesWonAfterShootouts)),
            new RankingColumnComputerWrapper(new GamesDrawnColumnComputer(), nameof(MyClubResources.GamesDrawn)),
            new RankingColumnComputerWrapper(new GamesLostColumnComputer(), nameof(MyClubResources.GamesLost)),
            new RankingColumnComputerWrapper(new GamesLostAfterShootoutsColumnComputer(), nameof(MyClubResources.GamesLostAfterShootouts)),
            new RankingColumnComputerWrapper(new GamesWithdrawnColumnComputer(), nameof(MyClubResources.GamesWithdrawn)),
            new RankingColumnComputerWrapper(new GoalsForColumnComputer(), nameof(MyClubResources.GoalsFor)),
            new RankingColumnComputerWrapper(new GoalsAgainstColumnComputer(), nameof(MyClubResources.GoalsAgainst)),
            new RankingColumnComputerWrapper(new GoalsDifferenceColumnComputer(), nameof(MyClubResources.GoalsDifference)),
        ];

        [HasUniqueItems]
        [Display(Name = nameof(Penalties), ResourceType = typeof(MyClubResources))]
        public UiObservableCollection<EditableTeamPenaltyViewModel> Penalties { get; } = [];

        [HasUniqueItems]
        [Display(Name = nameof(Labels), ResourceType = typeof(MyClubResources))]
        public EditableRankingLabelsViewModel Labels { get; } = new();

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<ITeamViewModel> Teams { get; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ShowShootouts { get; set; }

        public ICommand AddTeamPenaltyCommand { get; private set; }

        public ICommand RemoveTeamPenaltyCommand { get; private set; }

        protected override void ResetCore() => Load(RankingRules.Default, [], [], false);

        public void Load(RankingRules rankingRules,
                         Dictionary<AcceptableValueRange<int>, RankLabel> labels,
                         Dictionary<Guid, int> penaltyPoints,
                         bool showShootouts)
        {
            ShowShootouts = showShootouts;

            PointsByGamesWon = GetPoints(rankingRules, MatchResultDetailled.Won);
            PointsByGamesWonAfterShootouts = GetPoints(rankingRules, MatchResultDetailled.WonAfterShootouts);
            PointsByGamesDrawn = GetPoints(rankingRules, MatchResultDetailled.Drawn);
            PointsByGamesLost = GetPoints(rankingRules, MatchResultDetailled.Lost);
            PointsByGamesLostAfterShootouts = GetPoints(rankingRules, MatchResultDetailled.LostAfterShootouts);
            PointsByGamesWithdrawn = GetPoints(rankingRules, MatchResultDetailled.Withdrawn);

            var availableRankingRowComparers = GetAvailableRankingRowComparers();

            RankingRowComparers.Set(rankingRules.Comparer.Select(x => availableRankingRowComparers.Find(y => y.Item.GetType() == x.GetType())).NotNull());
            UnusedRankingRowComparers.Set(availableRankingRowComparers.Except(RankingRowComparers));

            RankingRowComparers.Union(UnusedRankingRowComparers).ForEach(x => x.IsEnabled = ShowShootouts || x.DisplayName.Key != nameof(MyClubResources.SortingByGamesWonAfterShootouts) && x.DisplayName.Key != nameof(MyClubResources.SortingByGamesLostAfterShootouts));

            RankingColumnComputers.ForEach(x =>
            {
                x.IsActive = rankingRules.Computers.ContainsKey(x.DisplayName.Key);
                x.IsEnabled = ShowShootouts || x.DisplayName.Key != nameof(MyClubResources.GamesWonAfterShootouts) && x.DisplayName.Key != nameof(MyClubResources.GamesLostAfterShootouts);
            });

            Labels.Load(labels);

            Penalties.Set(penaltyPoints.Select(x => new EditableTeamPenaltyViewModel(_teams.GetById(x.Key))
            {
                Points = x.Value
            }));
        }

        public Dictionary<Guid, int> CreatePenaltyPoints() => Penalties.ToDictionary(x => x.Team.Id, x => x.Points);

        public Dictionary<AcceptableValueRange<int>, RankLabel> CreateLabels() => Labels.ToDictionary(x => new AcceptableValueRange<int>(x.Range.Start, x.Range.End), x => new RankLabel(x.Color?.ToString(), x.Name, x.ShortName, x.Description));

        public RankingRules Create()
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

            return new RankingRules(pointsByResult, new RankingComparer(RankingRowComparers.Select(x => x.Item)), RankingColumnComputers.Where(x => x.IsEnabled && x.IsActive).ToDictionary(x => x.DisplayName.Key, x => x.Item));
        }

        private static int? GetPoints(RankingRules rankingRules, MatchResultDetailled result) => rankingRules.PointsNumberByResult.ContainsKey(result) ? rankingRules.GetPoints(result) : null;
    }

    internal class RankingColumnComputerWrapper : EditableWrapper<IRankingColumnComputer>
    {
        public RankingColumnComputerWrapper(IRankingColumnComputer item, string resourceKey) : base(item) => DisplayName = new TranslatableString(resourceKey);

        public bool IsEnabled { get; set; } = true;

        public bool IsActive { get; set; }

        public TranslatableString DisplayName { get; }
    }

    internal class RankingRowComparerWrapper : EditableWrapper<RankingRowComparer>
    {
        public RankingRowComparerWrapper(RankingRowComparer item, string resourceKey) : base(item) => DisplayName = new TranslatableString(resourceKey);

        public bool IsEnabled { get; set; } = true;

        public TranslatableString DisplayName { get; }
    }
}

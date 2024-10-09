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

        private readonly UiObservableCollection<IEditableTeamViewModel> _teams = [];

        public EditableRankingRulesViewModel(ISourceProvider<IEditableTeamViewModel> teamsProvider)
        {
            Teams = new(_teams);

            AddTeamPenaltyCommand = CommandsManager.CreateNotNull<IEditableTeamViewModel>(x => Penalties.Add(new EditableTeamPenaltyViewModel(x)), x => !Penalties.Select(x => x.Team).Contains(x));
            RemoveTeamPenaltyCommand = CommandsManager.CreateNotNull<EditableTeamPenaltyViewModel>(x => Penalties.Remove(x), x => x is not null);

            Disposables.AddRange(
            [
                teamsProvider.Connect().AutoRefresh(x => x.Name).Sort(SortExpressionComparer<IEditableTeamViewModel>.Ascending(x => x.Name)).Bind(_teams).Subscribe(),
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
        public ReadOnlyObservableCollection<IEditableTeamViewModel> Teams { get; }

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

            PointsByGamesWon = GetPoints(rankingRules, ExtendedResult.Won);
            PointsByGamesWonAfterShootouts = GetPoints(rankingRules, ExtendedResult.WonAfterShootouts);
            PointsByGamesDrawn = GetPoints(rankingRules, ExtendedResult.Drawn);
            PointsByGamesLost = GetPoints(rankingRules, ExtendedResult.Lost);
            PointsByGamesLostAfterShootouts = GetPoints(rankingRules, ExtendedResult.LostAfterShootouts);
            PointsByGamesWithdrawn = GetPoints(rankingRules, ExtendedResult.Withdrawn);

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
            var pointsByResult = new Dictionary<ExtendedResult, int>();

            if (PointsByGamesWon.HasValue)
                pointsByResult.Add(ExtendedResult.Won, PointsByGamesWon.Value);

            if (PointsByGamesWonAfterShootouts.HasValue)
                pointsByResult.Add(ExtendedResult.WonAfterShootouts, PointsByGamesWonAfterShootouts.Value);

            if (PointsByGamesDrawn.HasValue)
                pointsByResult.Add(ExtendedResult.Drawn, PointsByGamesDrawn.Value);

            if (PointsByGamesLost.HasValue)
                pointsByResult.Add(ExtendedResult.Lost, PointsByGamesLost.Value);

            if (PointsByGamesLostAfterShootouts.HasValue)
                pointsByResult.Add(ExtendedResult.LostAfterShootouts, PointsByGamesLostAfterShootouts.Value);

            if (PointsByGamesWithdrawn.HasValue)
                pointsByResult.Add(ExtendedResult.Withdrawn, PointsByGamesWithdrawn.Value);

            return new RankingRules(pointsByResult, new RankingComparer(RankingRowComparers.Select(x => x.Item)), RankingColumnComputers.Where(x => x.IsEnabled && x.IsActive).ToDictionary(x => x.DisplayName.Key, x => x.Item));
        }

        private static int? GetPoints(RankingRules rankingRules, ExtendedResult result) => rankingRules.PointsNumberByResult.ContainsKey(result) ? rankingRules.GetPoints(result) : null;
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

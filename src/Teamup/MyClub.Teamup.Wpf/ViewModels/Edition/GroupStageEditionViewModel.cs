// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;
using MyNet.Wpf.Extensions;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class GroupStageEditionViewModel : EntityEditionViewModel<IRound, RoundDto, RoundService>
    {
        private readonly LeaguePresentationService _leaguePresentationService;

        public GroupStageEditionViewModel(RoundService roundService, LeaguePresentationService leaguePresentationService) : base(roundService)
        {
            _leaguePresentationService = leaguePresentationService;

            var teamsChanged = new Subject<Func<EditableTeamViewModel, bool>>();
            PenaltiesTeamSelectionViewModel = new(teamsChanged);

            AddGroupCommand = CommandsManager.Create(AddGroup);
            RemoveGroupCommand = CommandsManager.CreateNotNull<EditableGroupViewModel>(RemoveGroup);
            AddPenaltyForSelectedTeamCommand = CommandsManager.Create(ValidateAndAddPenaltyTeam, () => PenaltiesTeamSelectionViewModel.SelectedItem is not null);
            RemoveTeamPenaltyCommand = CommandsManager.CreateNotNull<EditableTeamPenaltyViewModel>(x => Penalties.Remove(x), x => x is not null);
            RemoveRankLabelCommand = CommandsManager.CreateNotNull<EditableRankLabelViewModel>(x => RankingLabels.Remove(x), x => x is not null);
            EditRankLabelCommand = CommandsManager.CreateNotNull<EditableRankLabelViewModel>(async x => await EditRankLabelAsync(x).ConfigureAwait(false), x => x is not null);
            AddRankLabelCommand = CommandsManager.Create(async () => await AddRankLabelAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                Penalties.ToObservableChangeSet().Subscribe(_ => teamsChanged.OnNext(x => !Penalties.Select(x => x.Team).Contains(x))),
            ]);
        }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public CupViewModel? Parent { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public UiObservableCollection<TeamViewModel> AvailableTeams { get; } = [];

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public virtual string Name { get; set; } = string.Empty;

        [IsRequired]
        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]

        public virtual string ShortName { get; set; } = string.Empty;

        [IsRequired]
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [ValidateProperty(nameof(StartDate))]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateTime? EndDate { get; set; }

        public UiObservableCollection<EditableGroupViewModel> Groups { get; } = [];

        [Display(Name = nameof(PointsByGamesWon), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public int PointsByGamesWon { get; set; }

        [Display(Name = nameof(PointsByGamesDrawn), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public int PointsByGamesDrawn { get; set; }

        [Display(Name = nameof(PointsByGamesLost), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public int PointsByGamesLost { get; set; }

        [HasUniqueItems]
        [Display(Name = nameof(SortingColumns), ResourceType = typeof(MyClubResources))]
        public ObservableCollection<RankingSortingColumn> SortingColumns { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ObservableCollection<RankingSortingColumn> UnusedSortingColumns { get; } = [];

        [HasUniqueItems]
        [Display(Name = nameof(Penalties), ResourceType = typeof(MyClubResources))]
        public UiObservableCollection<EditableTeamPenaltyViewModel> Penalties { get; } = [];

        [HasUniqueItems]
        [Display(Name = nameof(SortingColumns), ResourceType = typeof(MyClubResources))]
        public UiObservableCollection<EditableRankLabelViewModel> RankingLabels { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public EditableTeamSelectionViewModel PenaltiesTeamSelectionViewModel { get; }

        public TimeSpan MatchTime { get; set; }

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        public ICommand AddPenaltyForSelectedTeamCommand { get; private set; }

        public ICommand RemoveTeamPenaltyCommand { get; private set; }

        public ICommand AddRankLabelCommand { get; private set; }

        public ICommand EditRankLabelCommand { get; private set; }

        public ICommand RemoveRankLabelCommand { get; private set; }

        public ICommand AddGroupCommand { get; private set; }

        public ICommand RemoveGroupCommand { get; private set; }


        private void RemoveGroup(EditableGroupViewModel group)
        {
            AvailableTeams.AddRange(group.Teams);

            Groups.Remove(group);
        }

        private void AddGroup()
        {
            var name = MyClubResources.Group.Increment(Groups.Select(x => x.Name).ToList(), format: " #");
            var group = new EditableGroupViewModel
            {
                Name = name,
                ShortName = name.GetInitials(),
                Order = Groups.MaxOrDefault(x => x.Order) + 1,
            };
            Groups.Add(group);
        }

        public void New(CupViewModel parent, Action? initialize = null)
        {
            Parent = parent;
            New(initialize);
            UpdateTitle();
            AvailableTeams.Set(parent.GetAvailableTeams());
            PenaltiesTeamSelectionViewModel.UpdateSource(parent.GetAvailableTeams());
        }

        public void Load(CupViewModel parent, Guid matchdayId)
        {
            Load(matchdayId);
            Parent = parent;
            UpdateTitle();
            AvailableTeams.Set(parent.GetAvailableTeams());
            PenaltiesTeamSelectionViewModel.UpdateSource(parent.GetAvailableTeams());
        }

        protected override string CreateTitle() => Parent?.Name ?? string.Empty;

        protected override void ResetItem()
        {
            if (Parent is not null)
            {
                var defaultValues = CrudService.NewGroupStage(Parent.Id);
                StartDate = defaultValues.StartDate.Date;
                EndDate = defaultValues.EndDate.Date;
                ShortName = defaultValues.ShortName.OrEmpty();
                Name = defaultValues.Name.OrEmpty();
                Penalties.Clear();
                Groups.Set(defaultValues.Groups?.Select(x =>
                {
                    var group = new EditableGroupViewModel(x.Id)
                    {
                        Name = x.Name.OrEmpty(),
                        ShortName = x.ShortName.OrEmpty(),
                        Order = x.Order,
                    };
                    group.Teams.Set(Parent.GetAvailableTeams().Where(x => (defaultValues.TeamIds ?? []).Contains(x.Id)).ToList());

                    return group;
                }).ToList());

                if (defaultValues.Rules is LeagueRules leagueRules)
                {
                    MatchTime = leagueRules.MatchTime;
                    if (leagueRules.MatchFormat is not null)
                        MatchFormat.Load(leagueRules.MatchFormat);
                    else
                        MatchFormat.Reset();

                    PointsByGamesWon = leagueRules.RankingRules?.PointsByGamesWon ?? 0;
                    PointsByGamesLost = leagueRules.RankingRules?.PointsByGamesLost ?? 0;
                    PointsByGamesDrawn = leagueRules.RankingRules?.PointsByGamesDrawn ?? 0;
                    UnusedSortingColumns.Set(Enum.GetValues<RankingSortingColumn>().Except(leagueRules.RankingRules?.SortingColumns ?? []));
                    SortingColumns.Set(leagueRules.RankingRules?.SortingColumns ?? []);
                    RankingLabels.Set(leagueRules.RankingRules?.Labels?.Select(x => new EditableRankLabelViewModel
                    {
                        FromRank = x.Key.Min ?? 1,
                        ToRank = x.Key.Max ?? Groups.MaxOrDefault(x => x.Teams.Count),
                        Color = x.Value.Color?.ToColor(),
                        Description = x.Value.Description,
                        Name = x.Value.Name,
                        ShortName = x.Value.ShortName,
                    }) ?? []);
                }
            }
        }
        protected override RoundDto ToDto()
            => new GroupStageDto()
            {
                Id = ItemId,
                ParentId = Parent?.Id,
                Name = Name,
                ShortName = ShortName,
                Groups = Groups.Select(x => new GroupDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    Order = x.Order,
                    Penalties = Penalties.Where(y => x.Teams.Select(z => z.Id).Contains(y.Team.Id!.Value) && y.Points != 0).ToDictionary(y => y.Team.Id!.Value, y => y.Points),
                    TeamIds = x.Teams.Select(x => x.Id).ToList()
                }).ToList(),
                Rules = new LeagueRules(MatchFormat.Create(), new RankingRules(PointsByGamesWon, PointsByGamesDrawn, PointsByGamesLost, [.. SortingColumns], RankingLabels.ToDictionary(x => new AcceptableValueRange<int>(x.FromRank, x.ToRank), x => new RankLabel(x.Color?.ToHex(), x.Name, x.ShortName, x.Description))), MatchTime),
                StartDate = StartDate?.Date ?? DateTime.Today.BeginningOfYear(),
                EndDate = EndDate?.Date ?? DateTime.Today.EndOfYear(),
            };

        protected override void RefreshFrom(IRound item)
        {
            if (Parent is not null && item is GroupStage groupStage)
            {
                Name = groupStage.Name;
                ShortName = groupStage.ShortName;
                StartDate = groupStage.Period.Start.Date;
                EndDate = groupStage.Period.End.Date;
                MatchTime = groupStage.Rules.MatchTime;
                MatchFormat.Load(groupStage.Rules.MatchFormat);
                PenaltiesTeamSelectionViewModel.Reset();
                Groups.Set(groupStage.Groups.Select(x =>
                {
                    var group = new EditableGroupViewModel(x.Id)
                    {
                        Name = x.Name.OrEmpty(),
                        ShortName = x.ShortName.OrEmpty(),
                        Order = x.Order,
                    };
                    group.Teams.Set(Parent.GetAvailableTeams().Where(y => x.Teams.Select(z => z.Id).Contains(y.Id)).ToList());

                    return group;
                }).ToList());

                PointsByGamesWon = groupStage.Rules.RankingRules.PointsByGamesWon;
                PointsByGamesLost = groupStage.Rules.RankingRules.PointsByGamesLost;
                PointsByGamesDrawn = groupStage.Rules.RankingRules.PointsByGamesDrawn;
                Penalties.Set(groupStage.Groups.SelectMany(x => x.Penalties ?? new Dictionary<Team, int>()).NotNull().Select(x => new EditableTeamPenaltyViewModel(PenaltiesTeamSelectionViewModel.Source.First(y => y.Id == x.Key.Id)) { Points = x.Value }).ToArray());
                UnusedSortingColumns.Set(Enum.GetValues<RankingSortingColumn>().Except(groupStage.Rules.RankingRules.SortingColumns));
                SortingColumns.Set(groupStage.Rules.RankingRules.SortingColumns);
                RankingLabels.Set(groupStage.Rules.RankingRules.Labels.Select(x => new EditableRankLabelViewModel
                {
                    FromRank = x.Key.Min ?? 1,
                    ToRank = x.Key.Max ?? Groups.MaxOrDefault(x => x.Teams.Count),
                    Color = x.Value.Color?.ToColor(),
                    Description = x.Value.Description,
                    Name = x.Value.Name,
                    ShortName = x.Value.ShortName,
                }));
                Groups.SelectMany(x => x.Teams).ToList().ForEach(x => AvailableTeams.Remove(x));
            }
        }

        private void ValidateAndAddPenaltyTeam()
        {
            AddPenaltyForSelectedTeam();
            PenaltiesTeamSelectionViewModel.SelectedItem = null;
        }

        private void AddPenaltyForSelectedTeam()
        {
            if (PenaltiesTeamSelectionViewModel.SelectedItem is not null && !Penalties.Contains(PenaltiesTeamSelectionViewModel.SelectedItem))
                Penalties.Add(new EditableTeamPenaltyViewModel(PenaltiesTeamSelectionViewModel.SelectedItem));
        }

        private async Task AddRankLabelAsync()
        {
            var item = await _leaguePresentationService.CreateRankLabelAsync().ConfigureAwait(false);

            if (item is not null)
                RankingLabels.Add(item);
        }

        private async Task EditRankLabelAsync(EditableRankLabelViewModel oldItem) => await _leaguePresentationService.UpdateRankLabelAsync(oldItem).ConfigureAwait(false);
    }
}

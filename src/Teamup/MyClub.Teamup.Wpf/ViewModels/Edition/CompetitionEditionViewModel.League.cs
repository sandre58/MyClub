// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;
using MyNet.Wpf.Extensions;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class LeagueEditionViewModel : CompetitionEditionViewModel<League, LeagueSeason, LeagueDto>
    {
        private readonly LeaguePresentationService _leaguePresentationService;

        public LeagueEditionViewModel(CompetitionService service,
                                      TeamService teamService,
                                      TeamsProvider teamsProvider,
                                      LeaguePresentationService leaguePresentationService,
                                      TeamPresentationService teamPresentationService) : base(service, teamService, teamsProvider, teamPresentationService)
        {
            _leaguePresentationService = leaguePresentationService;

            var teamsChanged = new Subject<Func<EditableTeamViewModel, bool>>();
            PenaltiesTeamSelectionViewModel = new(teamPresentationService, teamsChanged);

            AddPenaltyForSelectedTeamCommand = CommandsManager.Create(ValidateAndAddPenaltyTeam, () => PenaltiesTeamSelectionViewModel.SelectedItem is not null);
            RemoveTeamPenaltyCommand = CommandsManager.CreateNotNull<EditableTeamPenaltyViewModel>(x => Penalties.Remove(x), x => x is not null);
            RemoveRankLabelCommand = CommandsManager.CreateNotNull<EditableRankLabelViewModel>(x => RankingLabels.Remove(x), x => x is not null);
            EditRankLabelCommand = CommandsManager.CreateNotNull<EditableRankLabelViewModel>(async x => await EditRankLabelAsync(x).ConfigureAwait(false), x => x is not null);
            AddRankLabelCommand = CommandsManager.Create(async () => await AddRankLabelAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                Teams.ToObservableChangeSet().OnItemRemoved(x => Penalties.RemoveMany(Penalties.Where(y => ReferenceEquals(x, y.Team)).ToList())).Subscribe(_ => PenaltiesTeamSelectionViewModel.UpdateSource(Teams)),
                Penalties.ToObservableChangeSet().Subscribe(_ => teamsChanged.OnNext(x => !Penalties.Select(x => x.Team).Contains(x))),
            ]);
        }

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
        public ThreadSafeObservableCollection<EditableTeamPenaltyViewModel> Penalties { get; } = [];

        [HasUniqueItems]
        [Display(Name = nameof(SortingColumns), ResourceType = typeof(MyClubResources))]
        public ThreadSafeObservableCollection<EditableRankLabelViewModel> RankingLabels { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public EditableTeamSelectionViewModel PenaltiesTeamSelectionViewModel { get; }

        public ICommand AddPenaltyForSelectedTeamCommand { get; private set; }

        public ICommand RemoveTeamPenaltyCommand { get; private set; }

        public ICommand AddRankLabelCommand { get; private set; }

        public ICommand EditRankLabelCommand { get; private set; }

        public ICommand RemoveRankLabelCommand { get; private set; }

        protected override LeagueDto CreateCompetitionDto() => new()
        {
            Penalties = Penalties.Where(x => x.Points != 0).ToDictionary(x => x.Team.Id ?? x.Team.TemporaryId, x => x.Points),
            Rules = new LeagueRules(MatchFormat.Create(), new RankingRules(PointsByGamesWon, PointsByGamesDrawn, PointsByGamesLost, [.. SortingColumns], RankingLabels.ToDictionary(x => new AcceptableValueRange<int>(x.FromRank, x.ToRank), x => new RankLabel(x.Color?.ToHex(), x.Name, x.ShortName, x.Description))), MatchTime)
        };

        protected override void RefreshFromCompetition(LeagueSeason season)
        {
            PenaltiesTeamSelectionViewModel.Reset();

            PointsByGamesWon = season.Rules.RankingRules.PointsByGamesWon;
            PointsByGamesLost = season.Rules.RankingRules.PointsByGamesLost;
            PointsByGamesDrawn = season.Rules.RankingRules.PointsByGamesDrawn;
            Penalties.Set(season.Penalties?.Select(x => new EditableTeamPenaltyViewModel(PenaltiesTeamSelectionViewModel.Source.First(y => y.Id == x.Key.Id)) { Points = x.Value }).ToArray() ?? []);
            UnusedSortingColumns.Set(Enum.GetValues<RankingSortingColumn>().Except(season.Rules.RankingRules.SortingColumns));
            SortingColumns.Set(season.Rules.RankingRules.SortingColumns);
            RankingLabels.Set(season.Rules.RankingRules.Labels.Select(x => new EditableRankLabelViewModel
            {
                FromRank = x.Key.Min ?? 1,
                ToRank = x.Key.Max ?? Teams.Count,
                Color = x.Value.Color?.ToColor(),
                Description = x.Value.Description,
                Name = x.Value.Name,
                ShortName = x.Value.ShortName,
            }));
        }

        protected override void ResetItem()
        {
            PenaltiesTeamSelectionViewModel.Reset();

            var defaultValues = CrudService.NewLeague();
            StartDate = defaultValues.StartDate.Date;
            EndDate = defaultValues.EndDate.Date;
            Category = defaultValues.Category;
            Name = defaultValues.Name.OrEmpty();
            ShortName = defaultValues.ShortName.OrEmpty();
            Logo = defaultValues.Logo;
            Teams.Set(defaultValues.Teams is not null ? TeamSelectionViewModel.Source.Where(x => x.Id is not null && defaultValues.Teams.Select(y => y.Id).Contains(x.Id)).ToList() : Array.Empty<EditableTeamViewModel>());
            Penalties.Set(defaultValues.Penalties?.Select(x => new EditableTeamPenaltyViewModel(PenaltiesTeamSelectionViewModel.Source.First(y => y.Id == x.Key)) { Points = x.Value }) ?? []);

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
                    ToRank = x.Key.Max ?? Teams.Count,
                    Color = x.Value.Color?.ToColor(),
                    Description = x.Value.Description,
                    Name = x.Value.Name,
                    ShortName = x.Value.ShortName,
                }) ?? []);
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

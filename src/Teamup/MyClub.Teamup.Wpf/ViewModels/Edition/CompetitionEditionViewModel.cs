// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.Wpf.Extensions;
using MyNet.Utilities;
using MyNet.Observable.Collections;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal abstract class CompetitionEditionViewModel<T, TSeason, TDto> : EntityEditionViewModel<ICompetitionSeason, CompetitionDto, CompetitionService>
        where T : ICompetition
        where TSeason : ICompetitionSeason
        where TDto : CompetitionDto
    {
        private readonly TeamsProvider _teamsProvider;
        private readonly TeamService _teamService;
        private readonly TeamPresentationService _teamPresentationService;

        protected CompetitionEditionViewModel(CompetitionService service,
                                              TeamService teamService,
                                              TeamsProvider teamsProvider,
                                              TeamPresentationService teamPresentationService)
            : base(service)
        {
            _teamsProvider = teamsProvider;
            _teamService = teamService;
            _teamPresentationService = teamPresentationService;

            var teamsChanged = new Subject<Func<EditableTeamViewModel, bool>>();
            TeamSelectionViewModel = new(teamPresentationService, teamsChanged);

            ValidationRules.AddNotNull<CompetitionEditionViewModel<T, TSeason, TDto>, ThreadSafeObservableCollection<EditableTeamViewModel>>(x => x.Teams, MyClubResources.AnySelectedSquadsError, x => x.Any(x => x.IsMyTeam));
            ValidationRules.AddNotNull<CompetitionEditionViewModel<T, TSeason, TDto>, DateTime?>(x => x.StartDate, MessageResources.FieldStartDateMustBeLowerOrEqualsThanEndDateError, x => x.HasValue && EndDate.HasValue && x.Value.BeginningOfDay() <= EndDate.Value.EndOfDay());
            ValidationRules.AddNotNull<CompetitionEditionViewModel<T, TSeason, TDto>, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => x.HasValue && StartDate.HasValue && StartDate.Value.BeginningOfDay() <= x.Value.EndOfDay());

            AddSelectedTeamCommand = CommandsManager.Create(ValidateAndAddTeam, () => TeamSelectionViewModel.SelectedItem is not null || !string.IsNullOrEmpty(TeamSelectionViewModel.TextSearch));
            RemoveTeamCommand = CommandsManager.CreateNotNull<EditableTeamViewModel>(x => Teams.Remove(x), x => x is not null);
            EditTeamCommand = CommandsManager.CreateNotNull<EditableTeamViewModel>(async x => await TeamSelectionViewModel.EditAsync(x).ConfigureAwait(false), x => x is not null);
            ImportTeamsCommand = CommandsManager.Create(async () => await ImportTeamsAsync().ConfigureAwait(false));
            ExportTeamsCommand = CommandsManager.Create(async () => await ExportTeamsAsync().ConfigureAwait(false), () => Teams.Any());

            Disposables.AddRange(
            [
                Teams.ToObservableChangeSet().Subscribe(_ => teamsChanged.OnNext(x => !Teams.Contains(x))),
                this.WhenPropertyChanged(x => x.StartDate).Subscribe(_ =>
                {
                    if (StartDate.HasValue && StartDate >= EndDate)
                        EndDate = StartDate.Value.AddMonths(1);
                })
            ]);

            TeamSelectionViewModel.TeamCreated += OnTeamCreated;
        }

        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Name { get; set; } = string.Empty;

        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string ShortName { get; set; } = string.Empty;

        public byte[]? Logo { get; set; }

        public TimeSpan MatchTime { get; set; }

        [IsRequired]
        [Display(Name = nameof(Category), ResourceType = typeof(MyClubResources))]
        public Category? Category { get; set; }

        [IsRequired]
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [ValidateProperty(nameof(StartDate))]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateTime? EndDate { get; set; }

        [HasUniqueItems]
        [Display(Name = nameof(Teams), ResourceType = typeof(MyClubResources))]
        public ThreadSafeObservableCollection<EditableTeamViewModel> Teams { get; } = [];

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public EditableTeamSelectionViewModel TeamSelectionViewModel { get; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ShowRemovingTeamWarning { get; set; }

        public ICommand AddSelectedTeamCommand { get; private set; }

        public ICommand RemoveTeamCommand { get; private set; }

        public ICommand EditTeamCommand { get; private set; }

        public ICommand ImportTeamsCommand { get; private set; }

        public ICommand ExportTeamsCommand { get; private set; }

        private void OnTeamCreated(object? sender, EditableTeamViewModel team)
        {
            Teams.Add(team);
            TeamSelectionViewModel.SelectedItem = null;
        }

        private void ValidateAndAddTeam()
        {
            AddSelectedTeam();
            TeamSelectionViewModel.SelectedItem = null;
            TeamSelectionViewModel.TextSearch = null;
        }

        private void AddSelectedTeam()
        {
            if (TeamSelectionViewModel.SelectedItem is not null)
            {
                if (!Teams.Contains(TeamSelectionViewModel.SelectedItem))
                    Teams.Add(TeamSelectionViewModel.SelectedItem);
            }
            else if (!string.IsNullOrEmpty(TeamSelectionViewModel.TextSearch))
            {
                var defaultValue = _teamService.NewTeam();
                Teams.Add(new EditableTeamViewModel
                {
                    ClubName = TeamSelectionViewModel.TextSearch,
                    Name = TeamSelectionViewModel.TextSearch,
                    ShortName = TeamSelectionViewModel.TextSearch.GetInitials(),
                    AwayColor = defaultValue.AwayColor?.ToColor(),
                    HomeColor = defaultValue.HomeColor?.ToColor(),
                    Country = defaultValue.Club.OrThrow().Country,
                    IsMyTeam = false,
                    Category = defaultValue.Category,
                });
            }
        }

        private async Task ImportTeamsAsync()
        {
            var teamsToAdd = (await _teamPresentationService.LaunchImportAsync().ConfigureAwait(false)).ToList();

            // Update teams
            var updatedTeams = UpdateTeams(TeamSelectionViewModel.Source, teamsToAdd);

            // Add teams
            var teamNames = TeamSelectionViewModel.Source.Select(y => y.Name).ToList();
            var newTeams = teamsToAdd.Where(x => !teamNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            Teams.AddRange(newTeams.Concat(updatedTeams.Except(Teams)));
        }

        private async Task ExportTeamsAsync() => await _teamPresentationService.ExportAsync(Teams).ConfigureAwait(false);

        private static List<EditableTeamViewModel> UpdateTeams(IList<EditableTeamViewModel> originalTeams, IEnumerable<EditableTeamViewModel> newTeams)
        {
            var updatedTeams = new List<EditableTeamViewModel>();
            var originalTeamNames = originalTeams.Select(y => y.Name).ToList();

            newTeams.Where(x => originalTeamNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase)).ForEach(x =>
            {
                var similarTeam = originalTeams.FirstOrDefault(y => y.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));

                if (similarTeam != null)
                {
                    similarTeam.Name = x.Name;
                    similarTeam.ShortName = x.ShortName;
                    similarTeam.Logo = x.Logo;
                    similarTeam.Category = x.Category;
                    similarTeam.AwayColor = x.AwayColor;
                    similarTeam.HomeColor = x.HomeColor;
                    similarTeam.Country = x.Country;
                    similarTeam.Stadium = x.Stadium;

                    updatedTeams.Add(similarTeam);
                }
            });

            return updatedTeams;
        }

        protected override CompetitionDto ToDto()
        {
            var dto = CreateCompetitionDto();
            dto.Id = ItemId;
            dto.Name = Name;
            dto.ShortName = ShortName;
            dto.Logo = Logo;
            dto.Category = Category;
            dto.StartDate = StartDate?.Date ?? DateTime.Today.BeginningOfYear();
            dto.EndDate = EndDate?.Date ?? DateTime.Today.EndOfYear();
            dto.Teams = Teams.Select(x => new TeamDto
            {
                Id = x.Id,
                TemporaryId = x.TemporaryId,
                Name = x.Name,
                ShortName = x.ShortName,
                AwayColor = x.AwayColor?.ToHex(),
                HomeColor = x.HomeColor?.ToHex(),
                Category = x.Category,
                Stadium = x.Stadium is EditableStadiumViewModel stadium ?
                        new StadiumDto
                        {
                            Name = stadium.Name,
                            Address = stadium.Address,
                            Ground = stadium.Ground,
                            Id = stadium.Id
                        } : null,
                Club = x.IsMyTeam ? null
                : new ClubDto
                {
                    Logo = x.Logo,
                    AwayColor = x.AwayColor?.ToHex(),
                    HomeColor = x.HomeColor?.ToHex(),
                    Name = x.ClubName,
                    Country = x.Country,
                    Stadium = x.Stadium is EditableStadiumViewModel stadium1 ?
                        new StadiumDto
                        {
                            Name = stadium1.Name,
                            Address = stadium1.Address,
                            Ground = stadium1.Ground,
                            Id = stadium1.Id
                        } : null
                }
            }).ToList();

            return dto;
        }

        protected abstract TDto CreateCompetitionDto();

        protected override void RefreshFrom(ICompetitionSeason item)
        {
            TeamSelectionViewModel.Reset();
            TeamSelectionViewModel.UpdateSource(_teamsProvider.Items);

            Name = item.Competition.Name;
            ShortName = item.Competition.ShortName;
            Logo = item.Competition.Logo;
            Category = item.Competition.Category;
            StartDate = item.Period.Start.Date;
            EndDate = item.Period.End.Date;
            Teams.Set(TeamSelectionViewModel.Source.Where(x => x.Id is not null && item.Teams.Select(y => y.Id).Contains(x.Id)).ToList());
            MatchTime = item.Rules.MatchTime;
            MatchFormat.Load(item.Rules.MatchFormat);
            ShowRemovingTeamWarning = item.GetAllMatches().Any();

            RefreshFromCompetition((TSeason)item);
        }

        protected override void ResetCore()
        {
            TeamSelectionViewModel.Reset();
            TeamSelectionViewModel.UpdateSource(_teamsProvider.Items);

            base.ResetCore();
            ShowRemovingTeamWarning = false;
        }

        protected abstract void RefreshFromCompetition(TSeason season);

        protected override void Cleanup()
        {
            base.Cleanup();
            TeamSelectionViewModel.TeamCreated -= OnTeamCreated;
        }
    }
}

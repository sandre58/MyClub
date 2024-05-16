// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.Deferring;
using MyNet.Utilities.Messaging;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Messages;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.Factories;
using MyClub.Teamup.Domain.MyTeamAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Services
{
    public class TeamService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IMyTeamDomainService _myTeamDomainService;
        private readonly IProjectRepository _projectRepository;
        private readonly StadiumService _stadiumService;
        private readonly Deferrer _stadiumsChangedDeferrer;
        private readonly Deferrer _myTeamsChangedDeferrer;
        private readonly InjuriesStatisticsRefreshDeferrer _injuriesStatisticsRefreshDeferrer;
        private readonly TrainingStatisticsRefreshDeferrer _trainingStatisticsRefreshDeferrer;

        public TeamService(ITeamRepository teamRepository, IMyTeamDomainService myTeamDomainService, IProjectRepository projectRepository, StadiumService stadiumService, InjuriesStatisticsRefreshDeferrer injuriesStatisticsRefreshDeferrer, TrainingStatisticsRefreshDeferrer trainingStatisticsRefreshDeferrer)
        {
            _teamRepository = teamRepository;
            _myTeamDomainService = myTeamDomainService;
            _projectRepository = projectRepository;
            _stadiumService = stadiumService;
            _injuriesStatisticsRefreshDeferrer = injuriesStatisticsRefreshDeferrer;
            _trainingStatisticsRefreshDeferrer = trainingStatisticsRefreshDeferrer;
            _stadiumsChangedDeferrer = new(() => Messenger.Default.Send(new StadiumsChangedMessage()));
            _myTeamsChangedDeferrer = new(() =>
            {
                Messenger.Default.Send(new TeamsOrderValidationMessage(_myTeamDomainService.ValidateOrder()));
                _injuriesStatisticsRefreshDeferrer.AskRefresh();
                _trainingStatisticsRefreshDeferrer.AskRefresh();
            });
        }

        public IEnumerable<Team> SaveMyTeams(IEnumerable<TeamDto> dtos, bool replaceOldItems = false)
        {
            using (_myTeamsChangedDeferrer.Defer())
                return Save(dtos, replaceOldItems);
        }

        public IEnumerable<Team> Save(IEnumerable<TeamDto> dtos, bool replaceOldItems = false)
        {
            using (DeferStadiumsChanged())
            {
                var oldList = GetMyTeams();
                var newIds = dtos.Select(x => x.Id).ToList();

                if (replaceOldItems)
                    oldList.Where(x => !newIds.Contains(x.Id)).ToList().ForEach(x => _projectRepository.GetCurrentOrThrow().Club.RemoveTeam(x));

                return dtos.Select(Save).ToList();
            }
        }

        public Team Save(TeamDto dto)
        {
            var result = !dto.Id.HasValue || GetById(dto.Id.Value) is not Team foundEntity
                        ? CreateTeam(dto)
                        : UpdateTeam(foundEntity, dto);

            _stadiumsChangedDeferrer.DeferOrExecute();

            if (result.Club.Id == _projectRepository.GetCurrentOrThrow().Club.Id)
                _myTeamsChangedDeferrer.DeferOrExecute();

            return result;
        }

        private Team CreateTeam(TeamDto dto)
        {
            var club = dto.Club is null
                ? _projectRepository.GetCurrentOrThrow().Club
                : GetClubByName(dto.Club.Name.OrEmpty()) is Club foundClub
                ? foundClub
                : new Club(dto.Club.Name.OrEmpty());

            var entity = club.AddTeam(dto.Category.OrThrow(), dto.Name, dto.ShortName);

            UpdateTeam(entity, dto);
            return entity;
        }

        private Team UpdateTeam(Team entity, TeamDto dto)
        {
            if (dto.Club is not null && entity.Club.Id != _projectRepository.GetCurrentOrThrow().Club.Id)
                UpdateClub(entity.Club, dto.Club);

            entity.Name = dto.Name.OrEmpty();
            entity.ShortName = dto.ShortName.OrEmpty();
            entity.Order = dto.Order;

            dto.HomeColor.IfNotNull(entity.HomeColor.Override, entity.HomeColor.Reset);
            dto.AwayColor.IfNotNull(entity.AwayColor.Override, entity.AwayColor.Reset);
            dto.Stadium.IfNotNull(x => _stadiumService.AssignStadium(dto.Stadium, entity.Stadium.Override), entity.Stadium.Reset);

            return entity;
        }

        private void UpdateClub(Club entity, ClubDto dto)
        {
            entity.Name = dto.Name.OrEmpty();
            entity.ShortName = dto.Name.OrEmpty();
            entity.Logo = dto.Logo;
            entity.Country = dto.Country;

            if (!string.IsNullOrEmpty(dto.AwayColor))
                entity.AwayColor = dto.AwayColor;
            if (!string.IsNullOrEmpty(dto.HomeColor))
                entity.HomeColor = dto.HomeColor;
            if (dto.Stadium != null)
                _stadiumService.AssignStadium(dto.Stadium, x => entity.Stadium = x);
        }

        public TeamDto NewTeam() => NewTeam(new ClubDto
        {
            Name = MyClubResources.Club,
            Country = _projectRepository.GetCurrentOrThrow().Club.Country,
        }, Array.Empty<(Category, string, int)>());

        public TeamDto NewMyTeam(IEnumerable<(Category category, string name, int order)> existingTeams) => NewTeam(null, existingTeams);

        private TeamDto NewTeam(ClubDto? club, IEnumerable<(Category category, string name, int order)> existingTeams)
        {
            var category = _projectRepository.GetCurrentOrThrow().Category;
            var clubName = club?.Name ?? _projectRepository.GetCurrentOrThrow().Club.ShortName;
            var name = TeamFactory.GetTeamName(clubName, existingTeams.Where(x => x.category == category).Select(x => x.name));
            return new()
            {
                Club = club,
                Name = name,
                ShortName = name.GetInitials(),
                Category = category,
                Order = existingTeams.MaxOrDefault(x => x.order) + 1
            };
        }

        public IEnumerable<Team> GetAll() => _teamRepository.GetAll();

        public IEnumerable<Team> GetMyTeams() => _projectRepository.GetCurrentOrThrow().Club.Teams;

        public Team? GetById(Guid id) => _projectRepository.GetCurrentOrThrow().Club.Teams.GetByIdOrDefault(id) ?? _teamRepository.GetById(id);

        public Club? GetClubByName(string name) => _teamRepository.GetAll().Select(x => x.Club).Concat([_projectRepository.GetCurrentOrThrow().Club]).FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public IList<Team> GetSimilarTeams(string name, Category category)
            => _teamRepository.GetAll().Where(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && x.Category == category).ToList();

        public IDisposable DeferStadiumsChanged() => _stadiumsChangedDeferrer.Defer();
    }
}

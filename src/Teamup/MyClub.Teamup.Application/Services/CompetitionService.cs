// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Teamup.Application.Services
{
    public class CompetitionService(ICompetitionSeasonRepository repository,
                                          IProjectRepository projectRepository,
                                          TeamService teamService) : CrudService<ICompetitionSeason, CompetitionDto, ICompetitionSeasonRepository>(repository)
    {
        private readonly TeamService _teamService = teamService;
        private readonly IProjectRepository _projectRepository = projectRepository;

        protected override ICompetitionSeason CreateEntity(CompetitionDto dto)
        {
            ICompetitionSeason newEntity;

            var season = _projectRepository.GetCurrentOrThrow().Season;
            switch (dto)
            {
                case LeagueDto _:
                    var league = new League(dto.Name.OrEmpty(),
                                            dto.ShortName.OrEmpty(),
                                            dto.Category.OrThrow(),
                                            dto.Rules as LeagueRules ?? LeagueRules.Default);

                    newEntity = new LeagueSeason(league, season, league.Rules, dto.StartDate, dto.EndDate);
                    break;
                case FriendlyDto _:
                    var friendly = new Friendly(dto.Name.OrEmpty(), dto.ShortName.OrEmpty(), dto.Category.OrThrow(), dto.Rules as FriendlyRules ?? FriendlyRules.Default);
                    newEntity = new FriendlySeason(friendly, season, friendly.Rules, dto.StartDate, dto.EndDate);
                    break;
                case CupDto _:
                    var cup = new Cup(dto.Name.OrEmpty(), dto.ShortName.OrEmpty(), dto.Category.OrThrow(), dto.Rules as CupRules ?? CupRules.Default);
                    newEntity = new CupSeason(cup, season, cup.Rules, dto.StartDate, dto.EndDate);
                    break;
                default:
                    throw new InvalidOperationException("Competition type is unknown");
            }

            UpdateEntity(newEntity, dto);

            return newEntity;
        }

        protected override void UpdateEntity(ICompetitionSeason entity, CompetitionDto dto)
        {
            entity.Competition.Name = dto.Name.OrEmpty();
            entity.Competition.ShortName = dto.ShortName.OrEmpty();
            entity.Competition.Logo = dto.Logo;

            entity.Period.SetInterval(dto.StartDate, dto.EndDate);

            if (dto.Rules is not null)
            {
                entity.Rules = dto.Rules;
                entity.Competition.Rules = dto.Rules;
            }

            var teamAssociations = new Dictionary<Guid, Team>();

            if (dto.Teams is not null)
            {
                using (_teamService.DeferStadiumsChanged())
                {
                    dto.Teams?.ForEach(x =>
                    {
                        var team = _teamService.Save(x);
                        team.IfNotNull(z => teamAssociations.Add(x.Id ?? x.TemporaryId ?? Guid.Empty, z));
                    });
                    entity.SetTeams(teamAssociations.Values);
                }
            }

            switch (entity)
            {
                case LeagueSeason leagueSeason:
                    if (dto is LeagueDto leagueDto && leagueDto.Penalties is not null)
                        leagueSeason.Penalties = leagueDto.Penalties.Where(x => teamAssociations.ContainsKey(x.Key)).ToDictionary(x => teamAssociations[x.Key], x => x.Value);
                    break;
            }
        }

        public IList<ICompetitionSeason> Import(IEnumerable<CompetitionDto> items)
        {
            var isSimilar = new Func<CompetitionDto, bool>(x => GetSimilarCompetition(x.Category, x.Name.OrEmpty()).Any());
            var competitions = items.Where(x => !isSimilar(x)).Select(Save).ToList();

            using (CollectionChangedDeferrer.Defer())
            {
                items.Where(isSimilar).ToList().ForEach(y =>
                {
                    var similarCompetition = GetSimilarCompetition(y.Category, y.Name.OrEmpty()).FirstOrDefault();

                    if (similarCompetition is not null)
                    {
                        var dto = y;
                        dto.Id = similarCompetition.Id;
                        var newCompetition = Save(dto);
                        competitions.Add(newCompetition);
                    }
                });
            }

            return competitions;
        }

        public IList<ICompetitionSeason> GetSimilarCompetition(Category? category, string name)
            => Repository.GetAll().Where(x => x.Competition.Category.Equals(category) && x.Competition.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).ToList();

        #region League

        public Ranking GetRankingByMatchday(Guid seasonId, Guid matchdayId)
        {
            var season = Repository.GetById(seasonId).OrThrow().CastIn<LeagueSeason>();
            var matchday = season.Matchdays.GetById(matchdayId);
            var matches = season.Matchdays.Where(x => x.OriginDate.IsBefore(matchday.OriginDate.AddDays(1))).SelectMany(x => x.Matches);
            return new Ranking(season.Teams, matches, season.Rules.RankingRules, season.Penalties);
        }

        public LeagueDto NewLeague()
        {
            var allNames = Repository.GetAll().Select(x => x.Competition.Name).Distinct();
            var name = MyClubResources.League.Increment(allNames, format: " #");

            var item = new LeagueDto()
            {
                Rules = LeagueRules.Default,
                Teams = _projectRepository.GetCurrentOrThrow().GetMainTeams().Select(x => new TeamDto { Id = x.Id }).ToList(),
                Name = name,
                ShortName = name.GetInitials(),
                StartDate = _projectRepository.GetCurrentOrThrow().Season.Period.Start,
                EndDate = _projectRepository.GetCurrentOrThrow().Season.Period.End,
                Category = _projectRepository.GetCurrentOrThrow().Category,
            };
            return item;
        }

        #endregion

        #region Friendly

        public FriendlyDto NewFriendly() => new()
        {
            Rules = FriendlyRules.Default,
            Teams = _projectRepository.GetCurrentOrThrow().GetMainTeams().Select(x => new TeamDto { Id = x.Id }).ToList(),
            Name = MyClubResources.Friendlies,
            ShortName = MyClubResources.Friendlies.Substring(0, 2).ToUpper(),
            StartDate = _projectRepository.GetCurrentOrThrow().Season.Period.Start,
            EndDate = _projectRepository.GetCurrentOrThrow().Season.Period.End,
            Category = _projectRepository.GetCurrentOrThrow().Category,
        };

        #endregion

        #region Cup

        public CupDto NewCup()
        {
            var allNames = Repository.GetAll().Select(x => x.Competition.Name).Distinct();
            var name = MyClubResources.Cup.Increment(allNames, format: " #");
            var item = new CupDto()
            {
                Rules = CupRules.Default,
                Teams = _projectRepository.GetCurrentOrThrow().GetMainTeams().Select(x => new TeamDto { Id = x.Id }).ToList(),
                Name = name,
                ShortName = name.GetInitials(),
                StartDate = _projectRepository.GetCurrentOrThrow().Season.Period.Start,
                EndDate = _projectRepository.GetCurrentOrThrow().Season.Period.End,
                Category = _projectRepository.GetCurrentOrThrow().Category,
            };

            return item;
        }

        #endregion
    }
}

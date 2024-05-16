// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyNet.Utilities;
using MyNet.Utilities.Messaging;
using MyClub.Application.Services;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Messages;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Services
{
    public class MatchService(IMatchRepository repository,
                              ICompetitionSeasonRepository competitionRepository,
                              ITeamRepository teamRepository,
                              StadiumService stadiumService) : CrudService<Match, MatchDto, IMatchRepository>(repository)
    {
        private readonly StadiumService _stadiumService = stadiumService;
        private readonly ITeamRepository _teamRepository = teamRepository;
        private readonly ICompetitionSeasonRepository _competitionRepository = competitionRepository;

        protected override Match CreateEntity(MatchDto dto)
        {
            var parent = _competitionRepository.GetMatchesParent(dto.ParentId.GetValueOrDefault());

            var homeTeam = _teamRepository.GetById(dto.HomeTeamId.GetValueOrDefault()).OrThrow();
            var awayTeam = _teamRepository.GetById(dto.AwayTeamId.GetValueOrDefault()).OrThrow();

            var entity = Repository.Insert(parent, dto.Date, homeTeam, awayTeam);

            UpdateEntity(entity, dto);
            return entity;
        }

        protected override void UpdateEntity(Match entity, MatchDto dto)
        {
            if (dto.Date != default)
                entity.OriginDate = dto.Date;
            entity.PostponedDate = dto.PostponedDate;
            _stadiumService.AssignStadium(dto.Stadium, z => entity.Stadium = z);
            entity.NeutralVenue = dto.NeutralVenue;

            if (dto.Format is not null)
                entity.Format = dto.Format;

            switch (dto.State)
            {
                case MatchState.None:
                    entity.Reset();
                    break;

                case MatchState.InProgress:
                case MatchState.Suspended:
                case MatchState.Played:

                    if (dto.State == MatchState.InProgress)
                        entity.Start();
                    else if (dto.State == MatchState.Suspended)
                        entity.Suspend();
                    else
                        entity.Reset();

                    if (dto.HomeIsWithdrawn)
                        entity.Home.DoWithdraw(dto.HomePenaltyPoints);
                    else if (dto.AwayIsWithdrawn)
                        entity.Away.DoWithdraw(dto.AwayPenaltyPoints);

                    entity.SetScore(dto.HomeScore.GetValueOrDefault(), dto.AwayScore.GetValueOrDefault(), dto.AfterExtraTime, dto.HomeShootoutScore, dto.AwayShootoutScore);

                    if (dto.State == MatchState.Played)
                        entity.Played();
                    break;

                case MatchState.Postponed:
                    entity.Postpone(dto.PostponedDate);
                    break;

                case MatchState.Cancelled:
                    entity.Cancel();
                    break;
            }
        }

        public void Start(Guid id) => Update(id, x => x.Start());

        public void Start(IEnumerable<Guid> matchIds)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(Start);
        }

        public void Suspend(Guid id) => Update(id, x => x.Suspend());

        public void Suspend(IEnumerable<Guid> matchIds)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(Suspend);
        }

        public void Reset(Guid id) => Update(id, x => x.Reset());

        public void Reset(IEnumerable<Guid> matchIds)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(Reset);
        }

        public void Postpone(Guid id, DateTime? postponedDate = null) => Update(id, x => x.Postpone(postponedDate));

        public void Postpone(IEnumerable<Guid> matchIds, DateTime? postponedDate = null)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(x => Postpone(x, postponedDate));
        }

        public void Cancel(Guid id) => Update(id, x => x.Cancel());

        public void Cancel(IEnumerable<Guid> matchIds)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(Cancel);
        }

        public void DoWithdraw(Guid id, Guid teamId) => Update(id, x =>
        {
            var teamOpponent = x.GetOpponent(teamId);

            if (teamOpponent is null) return;

            var otherOpponent = x.HomeTeam == teamOpponent.Team ? x.Away : x.Home;

            teamOpponent.DoWithdraw(1);
            teamOpponent.SetScore(0);

            otherOpponent.Reset();
            otherOpponent.SetScore(3);

            x.Played();
        });

        public void InvertTeams(Guid id) => Update(id, x => x.Invert());

        public MatchDto New(Guid parentId, DateTime date)
        {
            var parent = _competitionRepository.GetMatchesParent(parentId).OrThrow();

            return new()
            {
                Date = date,
                AfterExtraTime = false,
                AwayIsWithdrawn = false,
                AwayPenaltyPoints = 0,
                AwayScore = null,
                AwayShootoutScore = null,
                Format = parent.MatchFormat,
                HomeIsWithdrawn = false,
                HomePenaltyPoints = 0,
                HomeScore = null,
                HomeShootoutScore = null,
                NeutralVenue = false,
                PostponedDate = null,
                Stadium = null,
                State = MatchState.None,
            };
        }

        protected override void OnCollectionChanged()
        {
            base.OnCollectionChanged();

            Messenger.Default.Send(new StadiumsChangedMessage());
        }
    }
}

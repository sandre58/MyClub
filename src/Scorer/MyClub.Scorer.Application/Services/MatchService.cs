// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Application.Services;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Application.Services
{
    public class MatchService : CrudService<Match, MatchDto, IMatchRepository>
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IStadiumRepository _stadiumRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ISchedulingDomainService _availibilityCheckingDomainService;

        public MatchService(IMatchRepository repository,
                            IProjectRepository projectRepository,
                            ITeamRepository teamRepository,
                            IStadiumRepository stadiumRepository,
                            ISchedulingDomainService availibilityCheckingDomainService) : base(repository)
        {
            _teamRepository = teamRepository;
            _stadiumRepository = stadiumRepository;
            _projectRepository = projectRepository;
            _availibilityCheckingDomainService = availibilityCheckingDomainService;
        }

        protected override Match CreateEntity(MatchDto dto)
        {
            var homeTeam = _teamRepository.GetById(dto.HomeTeamId).OrThrow();
            var awayTeam = _teamRepository.GetById(dto.AwayTeamId).OrThrow();

            var entity = Repository.Insert(dto.StageId, dto.Date, homeTeam, awayTeam);

            UpdateEntity(entity, dto);
            return entity;
        }

        protected override void UpdateEntity(Match entity, MatchDto dto)
        {
            var schedulingParameters = entity.GetSchedulingParameters();
            if (dto.ScheduleAutomatic)
            {
                var matches = GetAll().ToList();
                var scheduledMatches = matches.Except([entity]).ToList();
                var startDate = scheduledMatches.Where(x => x.Date < dto.Date).MaxOrDefault(x => x.Date);
                schedulingParameters.Schedule([entity], startDate != DateTime.MinValue ? startDate.AddMinutes(1) : schedulingParameters.Start(), scheduledMatches);
            }
            else
            {
                if (dto.Date != default)
                    entity.OriginDate = dto.Date;
                entity.PostponedDate = dto.PostponedDate;
            }

            if (dto.ScheduleStadiumAutomatic)
            {
                var matches = GetAll().ToList();
                var scheduledMatches = matches.Except([entity]).ToList();
                schedulingParameters.ScheduleVenues([entity], _stadiumRepository.GetAll().ToList(), scheduledMatches);
            }
            else
            {
                entity.IsNeutralStadium = dto.IsNeutralStadium;
                UpdateStadium(entity, dto.Stadium);
            }

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

                    UpdateScore(entity, dto);
                    UpdateCards(entity, dto);

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

        public void UpdateRules(Guid id, MatchRules rules) => Update(id, x => x.Rules = rules);

        public void UpdateRules(IEnumerable<Guid> matchIds, MatchRules rules)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(x => UpdateRules(x, rules));
        }

        public void SaveScore(MatchDto dto) => Update(dto.Id!.Value, x => UpdateScore(x, dto));

        public void SaveScores(IEnumerable<MatchDto> matches)
        {
            using (CollectionChangedDeferrer.Defer())
                matches.ForEach(SaveScore);
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

        public void Finish(Guid id) => Update(id, x => x.Played());

        public void Finish(IEnumerable<Guid> matchIds)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(Finish);
        }

        public void Reschedule(Guid id, int offset, TimeUnit timeUnit) => Update(id, x => x.Schedule(offset, timeUnit));

        public void Reschedule(IEnumerable<Guid> matchIds, int offset, TimeUnit timeUnit)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(x => Reschedule(x, offset, timeUnit));
        }

        public void Reschedule(Guid id, DateTime date) => Update(id, x => x.Schedule(date));

        public void Reschedule(IEnumerable<Guid> matchIds, DateTime date)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(x => Reschedule(x, date));
        }

        public void Reschedule(MatchDto match) => match.Id.HasValue.IfTrue(() => Update(match.Id!.Value, x =>
        {
            x.Schedule(match.Date);
            SetStadium(x.Id, match.Stadium?.Id);
        }));

        public void Reschedule(IEnumerable<MatchDto> items)
        {
            using (CollectionChangedDeferrer.Defer())
                items.ForEach(Reschedule);
        }

        public void RescheduleAutomatic(Guid id) => RescheduleAutomatic([id]);

        public void RescheduleAutomatic(IEnumerable<Guid> matchIds)
        {
            using (CollectionChangedDeferrer.Defer())
            {
                var matches = GetAll().ToList();
                var schedulingMatches = matches.Where(x => matchIds.Contains(x.Id)).OrderBy(x => x.Date).ToList();
                var scheduledMatches = matches.Except(schedulingMatches).ToList();

                var schedulingMatchesGroupByParameters = schedulingMatches.GroupBy(x => x.GetSchedulingParameters());

                var startDate = scheduledMatches.Where(x => x.Date < schedulingMatches.MinOrDefault(x => x.Date)).MaxOrDefault(x => x.Date);
                schedulingMatchesGroupByParameters.ForEach(x =>
                {
                    x.Key.Schedule(x, startDate != DateTime.MinValue ? startDate : x.Key.Start(), scheduledMatches);

                    scheduledMatches.AddRange(x);
                    startDate = x.MaxOrDefault(x => x.Date);

                    // For audit
                    x.ForEach(x => Update(x.Id, y => { }));
                });
            }
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

        public void DoWithdraw(Guid id, Guid teamId)
            => Update(id, x =>
            {
                var team = _teamRepository.GetById(teamId).OrThrow();
                var teamOpponent = x.GetOpponent(team);

                if (teamOpponent is null) return;

                var otherOpponent = x.Home is not null && x.Home.Team == teamOpponent.Team ? x.Away : x.Home;

                if (otherOpponent is null) return;

                teamOpponent.DoWithdraw();
                teamOpponent.SetScore(0);

                otherOpponent.Reset();
                otherOpponent.SetScore(3);

                x.Played();
            });

        public void DoWithdraw(IEnumerable<(Guid id, Guid teamId)> matchIds)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(x => DoWithdraw(x.id, x.teamId));
        }

        public void AddGoal(Guid id, Guid teamId, GoalDto goal) => Update(id, x =>
        {
            var team = _teamRepository.GetById(teamId).OrThrow();
            var opponent = x.GetOpponent(team);

            if (opponent is null) return;

            opponent.AddGoal(new Goal(goal.Type,
                             goal.ScorerId.HasValue ? (opponent.Team).Players.GetByIdOrDefault(goal.ScorerId.Value) : null,
                             goal.AssistId.HasValue ? (opponent.Team).Players.GetByIdOrDefault(goal.AssistId.Value) : null,
                             goal.Minute));
        });

        public void AddHomeGoal(Guid id, GoalDto goal) => AddGoal(id, GetById(id).OrThrow().Home.OrThrow().Team.Id, goal);

        public void AddAwayGoal(Guid id, GoalDto goal) => AddGoal(id, GetById(id).OrThrow().Away.OrThrow().Team.Id, goal);

        public void RemoveGoal(Guid id, Guid teamId) => Update(id, x =>
        {
            var team = _teamRepository.GetById(teamId).OrThrow();
            var opponent = x.GetOpponent(team);

            if (opponent is null) return;

            if (opponent.GetScore() > 0)
                opponent.RemoveLastGoal();
        });

        public void RemoveHomeGoal(Guid id) => RemoveGoal(id, GetById(id).OrThrow().Home.OrThrow().Team.Id);

        public void RemoveAwayGoal(Guid id) => RemoveGoal(id, GetById(id).OrThrow().Away.OrThrow().Team.Id);

        public void AddPenaltyShootout(Guid id, Guid teamId, PenaltyShootoutDto penaltyShootout) => Update(id, x =>
        {
            var team = _teamRepository.GetById(teamId).OrThrow();
            var opponent = x.GetOpponent(team);

            if (opponent is null) return;

            opponent.AddPenaltyShootout(new PenaltyShootout(penaltyShootout.TakerId.HasValue ? opponent.Team.Players.GetByIdOrDefault(penaltyShootout.TakerId.Value) : null, penaltyShootout.Result));
        });

        public void AddHomePenaltyShootout(Guid id, PenaltyShootoutDto penaltyShootout) => AddPenaltyShootout(id, GetById(id).OrThrow().Home.OrThrow().Team.Id, penaltyShootout);

        public void AddAwayPenaltyShootout(Guid id, PenaltyShootoutDto penaltyShootout) => AddPenaltyShootout(id, GetById(id).OrThrow().Away.OrThrow().Team.Id, penaltyShootout);

        public void RemoveSucceededPenaltyShootout(Guid id, Guid teamId) => Update(id, x =>
        {
            if (x.Home is null || x.Away is null) return;

            var opponent = x.Home.Team.Id == teamId ? x.Home : x.Away.Team.Id == teamId ? x.Away : null;

            if (opponent is null) return;

            if (opponent.GetShootoutScore() > 0)
                opponent.RemoveLastSucceededPenaltyShootout();
        });

        public void RemoveHomePenaltyShootout(Guid id) => RemoveGoal(id, GetById(id).OrThrow().Home.OrThrow().Team.Id);

        public void RemoveAwayPenaltyShootout(Guid id) => RemoveGoal(id, GetById(id).OrThrow().Away.OrThrow().Team.Id);

        public void Randomize(Guid id) => Update(id, x => x.Randomize());

        public void Randomize(IEnumerable<Guid> matchIds)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(Randomize);
        }

        public void InvertTeams(Guid id) => Update(id, x =>
        {
            x.Invert();

            if (x.UseHomeVenue())
            {
                x.IsNeutralStadium = false;
                x.Stadium = x.Home?.Team.Stadium;
            }
        });

        public void InvertTeams(IEnumerable<Guid> matchIds)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(InvertTeams);
        }

        public void SetStadium(Guid id, Guid? stadiumId, bool? neutralVenue = null) => Update(id, x =>
        {
            if (neutralVenue.HasValue)
                x.IsNeutralStadium = neutralVenue.Value;
            UpdateStadium(x, stadiumId.HasValue ? new StadiumDto { Id = stadiumId } : null);
        });

        public void SetStadium(IEnumerable<Guid> matchIds, Guid? stadiumId, bool? neutralVenue = null)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(x => SetStadium(x, stadiumId, neutralVenue));
        }

        public void SetAutomaticStadium(Guid id) => SetAutomaticStadium([id]);

        public void SetAutomaticStadium(IEnumerable<Guid> matchIds)
        {
            using (CollectionChangedDeferrer.Defer())
            {
                var matches = GetAll().ToList();
                var schedulingMatches = matches.Where(x => matchIds.Contains(x.Id)).OrderBy(x => x.Date).ToList();
                var scheduledMatches = matches.Except(schedulingMatches).ToList();

                var stadiums = _stadiumRepository.GetAll();
                schedulingMatches.ForEach(x =>
                {
                    Update(x.Id, y => y.GetSchedulingParameters().ScheduleVenues([y], stadiums, scheduledMatches));

                    scheduledMatches.Add(x);
                });
            }
        }

        public MatchDto New(Guid stageId, DateTime date)
        {
            var stage = _projectRepository.GetCompetition().GetStage<IMatchesStage>(stageId) ?? throw new InvalidOperationException($"Match stage '{stageId}' not found");

            return new()
            {
                Date = date,
                AfterExtraTime = false,
                AwayIsWithdrawn = false,
                AwayScore = null,
                AwayShootoutScore = null,
                Format = stage.ProvideFormat(),
                Rules = stage.ProvideRules(),
                HomeIsWithdrawn = false,
                HomeScore = null,
                HomeShootoutScore = null,
                IsNeutralStadium = false,
                PostponedDate = null,
                Stadium = null,
                State = MatchState.None,
            };
        }

        public IEnumerable<(ConflictType, Guid, Guid?)> GetAllConflicts()
            => _availibilityCheckingDomainService.GetAllConflicts().Select(x => (x.Item1, x.Item2.Id, x.Item3?.Id));

        private void UpdateStadium(Match entity, StadiumDto? newStadiumDto)
        {
            var newStadium = newStadiumDto is null
                                    ? null
                                    : newStadiumDto.Id.HasValue
                                        ? _stadiumRepository.GetById(newStadiumDto.Id.Value)
                                        : _stadiumRepository.Insert(new Stadium(newStadiumDto.Name.OrEmpty(), newStadiumDto.Ground)
                                        {
                                            Address = newStadiumDto.Address
                                        });

            entity.Stadium = newStadium;
        }

        private static void UpdateScore(Match match, MatchDto dto)
        {
            if (match.Home is null || match.Away is null) return;

            if (dto.HomeIsWithdrawn)
                match.Home.DoWithdraw();
            else if (dto.AwayIsWithdrawn)
                match.Away.DoWithdraw();

            if (dto.HomeScore.HasValue || dto.AwayScore.HasValue || dto.HomeGoals is not null || dto.AwayGoals is not null)
            {
                if (dto.HomeGoals is not null || dto.AwayGoals is not null)
                    match.SetScore(dto.HomeGoals?.Select(x => toGoal(x, match.Home.Team)) ?? [],
                                   dto.AwayGoals?.Select(x => toGoal(x, match.Away.Team)) ?? [],
                                   dto.AfterExtraTime,
                                   dto.HomeShootout?.Select(x => toPenaltyShootout(x, match.Home.Team)),
                                   dto.AwayShootout?.Select(x => toPenaltyShootout(x, match.Away.Team)));
                else
                    match.SetScore(dto.HomeScore.GetValueOrDefault(), dto.AwayScore.GetValueOrDefault(), dto.AfterExtraTime, dto.HomeShootoutScore, dto.AwayShootoutScore);

                if (match.State is MatchState.None or MatchState.Cancelled or MatchState.Postponed)
                    match.Played();
            }
            else
                match.Reset();

            static Goal toGoal(GoalDto goal, IVirtualTeam team)
                => new(goal.Type, goal.ScorerId.HasValue && team is Team realTeam ? realTeam.Players.GetByIdOrDefault(goal.ScorerId.Value) : null, goal.AssistId.HasValue && team is Team realTeam1 ? realTeam1.Players.GetByIdOrDefault(goal.AssistId.Value) : null, goal.Minute);

            static PenaltyShootout toPenaltyShootout(PenaltyShootoutDto penalty, IVirtualTeam team)
                => new(penalty.TakerId.HasValue && team is Team realTeam ? realTeam.Players.GetByIdOrDefault(penalty.TakerId.Value) : null, penalty.Result);
        }

        private static void UpdateCards(Match match, MatchDto dto)
        {
            if (match.Home is null || match.Away is null) return;

            if (dto.HomeCards is not null)
                match.Home.SetCards(dto.HomeCards.Select(x => toCard(x, match.Home.Team)));

            if (dto.AwayCards is not null)
                match.Away.SetCards(dto.AwayCards.Select(x => toCard(x, match.Away.Team)));

            static Card toCard(CardDto card, IVirtualTeam team)
                => new(card.Color, card.PlayerId.HasValue && team is Team realTeam ? realTeam.Players.GetByIdOrDefault(card.PlayerId.Value) : null, card.Infraction, card.Minute);
        }

        protected override void OnCollectionChanged()
        {
            base.OnCollectionChanged();
            GetAll().ForEach(x => x.ComputeOpponents());
        }
    }
}

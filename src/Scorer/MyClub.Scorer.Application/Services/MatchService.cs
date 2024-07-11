// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Application.Services;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.Factories.Extensions;
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
        private readonly ISchedulingParametersRepository _schedulingParametersRepository;

        public MatchService(IMatchRepository repository,
                            IProjectRepository projectRepository,
                            ITeamRepository teamRepository,
                            IStadiumRepository stadiumRepository,
                            ISchedulingParametersRepository schedulingParametersRepository,
                            ISchedulingDomainService availibilityCheckingDomainService) : base(repository)
        {
            _teamRepository = teamRepository;
            _stadiumRepository = stadiumRepository;
            _projectRepository = projectRepository;
            _schedulingParametersRepository = schedulingParametersRepository;
            _availibilityCheckingDomainService = availibilityCheckingDomainService;
        }

        protected override Match CreateEntity(MatchDto dto)
        {
            var parent = _projectRepository.GetCompetition().GetAllMatchesProviders().GetByIdOrDefault(dto.ParentId) ?? throw new InvalidOperationException($"Match parent '{dto.ParentId}' not found");

            var homeTeam = _teamRepository.GetById(dto.HomeTeamId).OrThrow();
            var awayTeam = _teamRepository.GetById(dto.AwayTeamId).OrThrow();

            var entity = Repository.Insert(parent, dto.Date, homeTeam, awayTeam);

            UpdateEntity(entity, dto);
            return entity;
        }

        protected override void UpdateEntity(Match entity, MatchDto dto)
        {
            if (dto.Date != default)
                entity.OriginDate = dto.Date;
            entity.PostponedDate = dto.PostponedDate;
            entity.IsNeutralStadium = dto.IsNeutralStadium;
            UpdateStadium(entity, dto.Stadium);

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

                    UpdateScore(entity, dto);

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

        public IList<Match> Update(IEnumerable<Guid> matchIds, MatchMultipleDto dto)
        {
            using (CollectionChangedDeferrer.Defer())
            {
                var result = matchIds.Select(x => Update(x, y =>
                {
                    DateTime? date = null;
                    if (dto.UpdateDate && dto.Date != default)
                        date = dto.Date.ToUtcDateTime(y.OriginDate.TimeOfDay);

                    if (dto.UpdateTime && dto.Time != default)
                        date = (date ?? y.OriginDate).ToUtcDateTime(dto.Time);

                    if (date.HasValue)
                        y.OriginDate = date.Value;
                    else if (dto.Offset != 0)
                        y.Schedule(dto.Offset, dto.OffsetUnit);

                    if (dto.UpdateStadium)
                    {
                        y.IsNeutralStadium = dto.IsNeutralStadium;
                        UpdateStadium(y, dto.Stadium);
                    }
                })).ToList();

                return result;
            }
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
            x.Schedule(match.Date.ToUniversalTime());
            SetStadium(x.Id, match.Stadium?.Id);
        }));

        public void Reschedule(IEnumerable<MatchDto> items)
        {
            using (CollectionChangedDeferrer.Defer())
                items.ForEach(Reschedule);
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
                var teamOpponent = x.GetOpponent(teamId);

                if (teamOpponent is null) return;

                var otherOpponent = x.HomeTeam == teamOpponent.Team ? x.Away : x.Home;

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

        public void Randomize(Guid id) => Update(id, x => x.RandomizeScore());

        public void Randomize(IEnumerable<Guid> matchIds)
        {
            using (CollectionChangedDeferrer.Defer())
                matchIds.ForEach(Randomize);
        }

        public void InvertTeams(Guid id) => Update(id, x =>
        {
            x.Invert();

            if (_schedulingParametersRepository.GetByMatch(x).UseTeamVenues)
            {
                x.IsNeutralStadium = false;
                x.Stadium = x.HomeTeam.Stadium;
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

        public MatchDto New(Guid parentId, DateTime date)
        {
            var parent = _projectRepository.GetCompetition().GetAllMatchesProviders().GetByIdOrDefault(parentId) ?? throw new InvalidOperationException($"Match parent '{parentId}' not found");

            return new()
            {
                Date = date,
                AfterExtraTime = false,
                AwayIsWithdrawn = false,
                AwayScore = null,
                AwayShootoutScore = null,
                Format = parent.ProvideFormat(),
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
            if (dto.HomeIsWithdrawn)
                match.Home.DoWithdraw();
            else if (dto.AwayIsWithdrawn)
                match.Away.DoWithdraw();

            if (dto.HomeScore.HasValue || dto.AwayScore.HasValue)
            {
                match.SetScore(dto.HomeScore.GetValueOrDefault(), dto.AwayScore.GetValueOrDefault(), dto.AfterExtraTime, dto.HomeShootoutScore, dto.AwayShootoutScore);

                if (match.State is MatchState.None or MatchState.Cancelled or MatchState.Postponed)
                    match.Played();
            }
            else
                match.Reset();
        }
    }
}

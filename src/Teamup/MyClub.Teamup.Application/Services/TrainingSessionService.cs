// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyClub.Application.Services;
using MyClub.Teamup.Application.Dtos;
using MyClub.Application.Extensions;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TrainingAggregate;

namespace MyClub.Teamup.Application.Services
{
    public class TrainingSessionService(ITrainingSessionRepository repository,
                                  ISquadPlayerRepository playerRepository,
                                  IProjectRepository projectRepository,
                                  TrainingStatisticsRefreshDeferrer trainingStatisticsRefreshDeferrer) : CrudService<TrainingSession, TrainingSessionDto, ITrainingSessionRepository>(repository)
    {
        private readonly ISquadPlayerRepository _playerRepository = playerRepository;
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly TrainingStatisticsRefreshDeferrer _trainingStatisticsRefreshDeferrer = trainingStatisticsRefreshDeferrer;

        protected override TrainingSession CreateEntity(TrainingSessionDto dto)
        {
            var newEntity = new TrainingSession(dto.StartDate, dto.EndDate);
            UpdateEntity(newEntity, dto);

            return newEntity;
        }

        protected override void UpdateEntity(TrainingSession entity, TrainingSessionDto dto)
        {
            entity.SetDate(dto.StartDate, dto.EndDate);
            entity.Theme = dto.Theme.OrEmpty();
            entity.Place = dto.Place;

            if (dto.TechnicalGoals is not null) entity.TechnicalGoals.Set(dto.TechnicalGoals);
            if (dto.TacticalGoals is not null) entity.TacticalGoals.Set(dto.TacticalGoals);
            if (dto.MentalGoals is not null) entity.MentalGoals.Set(dto.MentalGoals);
            if (dto.PhysicalGoals is not null) entity.PhysicalGoals.Set(dto.PhysicalGoals);
            if (dto.TeamIds is not null) entity.TeamIds.Set(dto.TeamIds);
            if (dto.Attendances is not null) entity.Attendances.UpdateFrom(dto.Attendances, x => entity.AddAttendance(_playerRepository.GetByPlayerId(x.PlayerId).Player, x.Attendance, x.Rating, x.Reason, x.Comment), x => entity.RemoveAttendance(x.Player.Id), (x, y) =>
            {
                x.Attendance = y.Attendance;
                x.Rating = y.Rating;
                x.Reason = y.Reason;
                x.Comment = y.Comment;
            });
            if (dto.IsCancelled)
                entity.Cancel();
            else if (entity.IsCancelled)
                entity.UndoCancel();
        }

        public IList<TrainingSession> Update(IEnumerable<Guid> sessionIds, TrainingSessionMultipleDto dto)
        {
            using (CollectionChangedDeferrer.Defer())
            {
                return sessionIds.Select(x => Update(x, y =>
                {
                    if (dto.UpdateTheme)
                        y.Theme = dto.Theme.OrEmpty();

                    if (dto.StartDate != default || dto.EndDate != default)
                        y.SetDate(dto.StartDate != default ? y.Start.SetTime(dto.StartDate.TimeOfDay) : y.Start, dto.EndDate != default ? y.End.SetTime(dto.EndDate.TimeOfDay) : y.End);

                    if (dto.UpdatePlace)
                        y.Place = dto.Place;

                    if (dto.TeamIds != null)
                        y.TeamIds.Set(dto.TeamIds);
                })).ToList();
            }
        }

        public void Cancel(Guid id) => Update(id, x => x.Cancel());

        public void Cancel(IEnumerable<Guid> ids)
        {
            using (CollectionChangedDeferrer.Defer())
                ids.ForEach(Cancel);
        }

        public IList<TrainingSession> Add(IEnumerable<Period> dates, string place, string theme, IEnumerable<Guid> teamIds)
        {
            using (CollectionChangedDeferrer.Defer())
                return dates.Select(x => Save(new TrainingSessionDto
                {
                    StartDate = x.Start,
                    EndDate = x.End,
                    TeamIds = teamIds.ToList(),
                    Theme = theme,
                    Place = place
                })).ToList();
        }

        public void InitializeAttendances(Guid id) => Update(id, y => y.InitializeAttendances(_playerRepository.GetAll()));

        public void InitializeAttendances(IEnumerable<Guid> ids)
        {
            using (CollectionChangedDeferrer.Defer())
                ids.ForEach(InitializeAttendances);
        }

        public void RemoveAttendances(Guid trainingSessionId, IEnumerable<Guid> playerIds) => Update(trainingSessionId, x => x.RemoveAttendance(playerIds));

        public void SetAttendances(Guid trainingSessionId, IEnumerable<Guid> playerIds, Attendance attendance) => Update(trainingSessionId, x => x.Attendances.Where(y => playerIds.Contains(y.Player.Id)).ForEach(y => y.Attendance = attendance));

        protected override void OnCollectionChanged() => _trainingStatisticsRefreshDeferrer.AskRefresh();

        public TrainingSessionDto NewTrainingSession(DateTime date)
        {
            var project = _projectRepository.GetCurrentOrThrow();
            return new TrainingSessionDto()
            {
                StartDate = date.Date.AddFluentTimeSpan(project.Preferences.TrainingStartTime),
                EndDate = date.Date.AddFluentTimeSpan(project.Preferences.TrainingStartTime).AddFluentTimeSpan(project.Preferences.TrainingDuration),
                TeamIds = project.GetMainTeams().Select(x => x.Id).ToList(),
            };
        }
    }
}

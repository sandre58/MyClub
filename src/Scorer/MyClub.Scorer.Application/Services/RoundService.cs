// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Application.Services
{
    public class RoundService(IRoundRepository repository,
                              ICupRepository cupRepository,
                              IStadiumRepository stadiumRepository,
                              IProjectRepository projectRepository) : CrudService<Round, RoundDto, IRoundRepository>(repository)
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly ICupRepository _cupRepository = cupRepository;
        private readonly IStadiumRepository _stadiumRepository = stadiumRepository;

        protected override Round CreateEntity(RoundDto dto)
        {
            //var entity = dto.StageId.HasValue ? _cupRepository.InsertRound(OneLegFormat.Default, [], dto.Name.OrEmpty(), dto.ShortName) : throw new NotImplementedException();

            //UpdateEntity(entity, dto);

            //return entity;

            throw new NotImplementedException();
        }

        protected override void UpdateEntity(Round entity, RoundDto dto)
        {
            entity.Name = dto.Name.OrEmpty();
            entity.ShortName = dto.ShortName.OrEmpty();

            if (dto.SchedulingParameters is not null)
                entity.SchedulingParameters.Override(dto.SchedulingParameters);
            else
                entity.SchedulingParameters.Reset();

            if (dto.MatchRules is not null)
                entity.MatchRules.Override(dto.MatchRules);
            else
                entity.MatchRules.Reset();

            if (dto.Stages is not null)
            {
                var roundStagesToDelete = entity.Stages.Where(x => !dto.Stages.Select(x => x.Id).Contains(x.Id)).ToList();
                roundStagesToDelete.ForEach(x => entity.RemoveStage(x));

                foreach (var item in dto.Stages)
                {
                    var roundStage = item.Id.HasValue && entity.Stages.GetByIdOrDefault(item.Id.Value) is RoundStage r ? r : entity.AddStage(item.Date);
                    UpdateRoundStage(roundStage, item);
                }
            }
        }

        private void UpdateRoundStage(RoundStage entity, RoundStageDto dto)
        {
            var schedulingParameters = entity.ProvideSchedulingParameters();
            if (dto.ScheduleAutomatic)
            {
                var rounds = GetAll().ToList();
                var scheduledRoundStages = rounds.SelectMany(x => x.Stages).Except([entity]).ToList();
                var startDate = scheduledRoundStages.Where(x => x.Date < dto.Date).MaxOrDefault(x => x.Date);
                schedulingParameters.Schedule([entity], startDate != DateTime.MinValue ? startDate.AddMinutes(1) : schedulingParameters.Start(), scheduledRoundStages);
            }
            else
            {
                if (dto.IsPostponed || dto.PostponedDate.HasValue)
                    entity.Postpone(dto.PostponedDate, true);
                else if (dto.Date != default)
                    entity.ScheduleAll(dto.Date);
            }

            if (dto.ScheduleStadiumsAutomatic)
            {
                var matchdays = GetAll().ToList();
                var matches = entity.Matches.Where(x => x.State is MatchState.None or MatchState.Postponed).ToList();
                var scheduledMatches = GetAll().SelectMany(x => x.Stages).Where(x => x.Date < dto.Date).SelectMany(x => x.Matches).Except(matches).ToList();
                schedulingParameters.ScheduleVenues(matches, _stadiumRepository.GetAll().ToList(), scheduledMatches);
            }
        }

        public void Postpone(Guid id, DateTime? postponedDate = null)
        {
            var competition = _projectRepository.GetCompetition();

            Round entity;
            var roundStage = competition.GetStage<RoundStage>(id) ?? throw new InvalidOperationException($"Round stage '{id}' not found");
            roundStage.Postpone(postponedDate);

            entity = roundStage.Stage;

            Update(roundStage.Stage.Id, _ => roundStage.Postpone(postponedDate));
        }

        public void Postpone(IEnumerable<Guid> ids, DateTime? postponedDate = null)
        {
            using (CollectionChangedDeferrer.Defer())
                ids.ForEach(x => Postpone(x, postponedDate));
        }

        public RoundDto New(Guid? stageId = null)
        {
            var stage = GetStageOrCup(stageId);

            var name = MyClubResources.Round.Increment(stage.Rounds.Select(x => x.Name), format: " #");
            return new()
            {
                StageId = stageId,
                Name = name,
                ShortName = name.GetInitials(),
            };
        }

        public SchedulingParameters GetDefaultSchedulingParameters() => _cupRepository.GetCurrentOrThrow().SchedulingParameters;

        public MatchRules GetDefaultRules() => _cupRepository.GetCurrentOrThrow().MatchRules;

        private Knockout GetStageOrCup(Guid? stageId) => _projectRepository.GetCompetition() is not Cup competition
        ? throw new InvalidOperationException("Competition is not a cup")
        : (stageId.HasValue && stageId != competition.Id ? competition.GetStage<Knockout>(stageId) : competition) ?? throw new InvalidOperationException($"Matchday stage '{stageId}' not found");
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Application.Services
{
    public class RoundService(IRoundRepository repository,
                              ICupRepository cupRepository,
                              IProjectRepository projectRepository) : CrudService<IRound, RoundDto, IRoundRepository>(repository)
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly ICupRepository _cupRepository = cupRepository;

        protected override IRound CreateEntity(RoundDto dto)
        {
            var entity = !dto.StageId.HasValue ? _cupRepository.InsertRoundOfFixtures(dto.Name.OrEmpty(), dto.ShortName) : throw new NotImplementedException();

            UpdateEntity(entity, dto);

            return entity;
        }

        protected override void UpdateEntity(IRound entity, RoundDto dto)
        {
            switch (entity)
            {
                case RoundOfMatches roundOfMatches:
                    roundOfMatches.Name = dto.Name.OrEmpty();
                    roundOfMatches.ShortName = dto.ShortName.OrEmpty();

                    if (dto.IsPostponed || dto.PostponedDate.HasValue)
                        roundOfMatches.Postpone(dto.PostponedDate, true);
                    else if (dto.Date != default)
                        roundOfMatches.ScheduleAll(dto.Date);
                    break;
                case RoundOfFixtures roundOfFixtures:
                    roundOfFixtures.Name = dto.Name.OrEmpty();
                    roundOfFixtures.ShortName = dto.ShortName.OrEmpty();
                    break;
                default:
                    break;
            }
        }

        public RoundDto New(Guid? stageId = null)
        {
            var stage = _projectRepository.GetCompetition().GetStage<Knockout>(stageId) ?? throw new InvalidOperationException($"Round stage '{stageId}' not found");

            var name = MyClubResources.Round.Increment(stage.Rounds.Select(x => x.Name), format: " #");
            return new()
            {
                StageId = stageId,
                Name = name,
                ShortName = name.GetInitials(),
            };
        }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Application.Services;
using MyClub.Teamup.Application.Dtos;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.CycleAggregate;

namespace MyClub.Teamup.Application.Services
{
    public class CycleService(ICycleRepository repository) : CrudService<Cycle, CycleDto, ICycleRepository>(repository)
    {
        protected override Cycle CreateEntity(CycleDto dto)
        {
            var newEntity = new Cycle(dto.StartDate, dto.EndDate, dto.Label.OrEmpty());
            UpdateEntity(newEntity, dto);

            return newEntity;
        }

        protected override void UpdateEntity(Cycle entity, CycleDto dto)
        {
            entity.Period.SetInterval(dto.StartDate, dto.EndDate);
            entity.Label = dto.Label.OrEmpty();
            entity.Color = dto.Color;

            if (dto.TechnicalGoals is not null) entity.TechnicalGoals.Set(dto.TechnicalGoals);
            if (dto.TacticalGoals is not null) entity.TacticalGoals.Set(dto.TacticalGoals);
            if (dto.MentalGoals is not null) entity.MentalGoals.Set(dto.MentalGoals);
            if (dto.PhysicalGoals is not null) entity.PhysicalGoals.Set(dto.PhysicalGoals);
        }

        public int Remove(IEnumerable<DateTime> dates)
        {
            var cycles = Repository.GetAll().Where(x => dates.Any(y => x.Period.Contains(y))).ToList();

            return cycles.Count(x => Remove(x.Id));
        }

        public static CycleDto NewCycle(DateTime startDate, DateTime endDate) => new()
        {
            StartDate = startDate.BeginningOfDay(),
            EndDate = endDate.EndOfDay(),
            Label = MyClubResources.Cycle
        };

    }
}

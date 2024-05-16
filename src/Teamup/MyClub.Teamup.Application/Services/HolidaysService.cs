// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Application.Services;
using MyClub.Teamup.Application.Dtos;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.HolidaysAggregate;

namespace MyClub.Teamup.Application.Services
{
    public class HolidaysService(IHolidaysRepository repository) : CrudService<Holidays, HolidaysDto, IHolidaysRepository>(repository)
    {
        protected override Holidays CreateEntity(HolidaysDto dto)
        {
            var newEntity = new Holidays(dto.StartDate, dto.EndDate, dto.Label.OrEmpty());
            UpdateEntity(newEntity, dto);

            return newEntity;
        }

        protected override void UpdateEntity(Holidays entity, HolidaysDto dto)
        {
            entity.Period.SetInterval(dto.StartDate, dto.EndDate);
            entity.Label = dto.Label.OrEmpty();
        }

        public int Remove(IEnumerable<DateTime> dates)
        {
            var holidays = Repository.GetAll().Where(x => dates.Any(y => x.Period.Contains(y))).ToList();

            return holidays.Count(x => Remove(x.Id));
        }

        public static HolidaysDto NewHolidays(DateTime startDate, DateTime endDate) => new()
        {
            StartDate = startDate.BeginningOfDay(),
            EndDate = endDate.EndOfDay(),
            Label = MyClubResources.Holidays
        };
    }
}

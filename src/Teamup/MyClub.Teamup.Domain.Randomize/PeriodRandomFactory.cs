// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Helpers;

namespace MyClub.Teamup.Domain.Randomize
{
    public static class PeriodRandomFactory
    {
        public static (IEnumerable<Cycle> cycles, IEnumerable<Holidays> holidays) RandomPeriods(DateTime minDate, DateTime maxDate)
        {
            var cycles = new List<Cycle>();
            var holidays = new List<Holidays>();
            var countPeriods = RandomGenerator.Int(7, 10);
            var periodStartDate = minDate;
            for (var i = 0; i < countPeriods; i++)
            {
                if (periodStartDate >= maxDate) break;

                Period period;
                if (i % 2 == 0)
                {
                    var periodEndDate = periodStartDate.AddDays(RandomGenerator.Int(30, 60));
                    var cycle = RandomCycle(periodStartDate, periodEndDate < maxDate || i < countPeriods - 1 ? periodEndDate : maxDate);
                    cycles.Add(cycle);
                    period = cycle.Period;
                }
                else
                {
                    var periodEndDate = periodStartDate.AddDays(RandomGenerator.Int(5, 15));
                    var randomHolidays = RandomHolidays(periodStartDate, periodEndDate < maxDate || i < countPeriods - 1 ? periodEndDate : maxDate);
                    holidays.Add(randomHolidays);
                    period = randomHolidays.Period;
                }
                periodStartDate = period.End.AddDays(1);
            }

            return (cycles, holidays);
        }

        public static Holidays RandomHolidays(DateTime? startDate = null, DateTime? endDate = null)
        {
            var newStartDate = startDate ?? RandomGenerator.Date(DateTime.UtcNow.BeginningOfYear(), DateTime.UtcNow.EndOfYear());
            var newEndDate = endDate ?? RandomGenerator.Date(newStartDate, DateTime.UtcNow.EndOfYear());
            var item = new Holidays(newStartDate, newEndDate, SentenceGenerator.Words(1, 3));
            item.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            return item;
        }

        public static Cycle RandomCycle(DateTime? startDate = null, DateTime? endDate = null)
        {
            var newStartDate = startDate ?? RandomGenerator.Date(DateTime.UtcNow.BeginningOfYear(), DateTime.UtcNow.EndOfYear());
            var newEndDate = endDate ?? RandomGenerator.Date(newStartDate, DateTime.UtcNow.EndOfYear());
            var item = new Cycle(newStartDate, newEndDate, SentenceGenerator.Words(1, 2))
            {
                Color = RandomGenerator.Color()
            };

            EnumerableHelper.Iteration(RandomGenerator.Int(1, 4), _ => item.TechnicalGoals.Add(SentenceGenerator.Sentence(3, 5)));
            EnumerableHelper.Iteration(RandomGenerator.Int(1, 4), _ => item.TacticalGoals.Add(SentenceGenerator.Sentence(3, 5)));
            EnumerableHelper.Iteration(RandomGenerator.Int(1, 4), _ => item.PhysicalGoals.Add(SentenceGenerator.Sentence(3, 5)));
            EnumerableHelper.Iteration(RandomGenerator.Int(1, 4), _ => item.MentalGoals.Add(SentenceGenerator.Sentence(3, 5)));

            item.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            return item;
        }
    }
}

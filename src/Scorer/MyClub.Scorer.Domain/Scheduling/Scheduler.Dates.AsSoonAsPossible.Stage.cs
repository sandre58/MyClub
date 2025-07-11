// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class AsSoonAsPossibleStageScheduler : IDateScheduler<IMatchesStage>
    {
        private readonly List<Match> _scheduledItems = [];
        private DateTime _fromDate;

        public AsSoonAsPossibleStageScheduler(DateTime fromDate, IEnumerable<IMatchesStage>? scheduledItems = null)
        {
            _fromDate = fromDate;
            _scheduledItems = new(scheduledItems?.SelectMany(x => x.GetAllMatches()) ?? []);
        }

        public bool ScheduleVenues { get; set; } = true;

        public List<IAvailableDateSchedulingRule> Rules { get; set; } = [];

        public ICollection<Stadium> AvailableStadiums { get; set; } = [];

        public virtual void Schedule(IEnumerable<IMatchesStage> items)
        {
            items.SelectMany(x => x.GetAllMatches()).ToList().ForEach(x => _scheduledItems.Remove(x));

            items.ForEach(x =>
            {
                var scheduler = new AsSoonAsPossibleMatchesScheduler(_fromDate, _scheduledItems) { ScheduleVenues = ScheduleVenues, AvailableStadiums = AvailableStadiums, Rules = Rules };
                var matches = x.GetAllMatches().ToList();
                scheduler.Schedule(matches);

                _scheduledItems.AddRange(matches);
                var dateOfMatchday = matches.MinOrDefault(x => x.Date, x.Date);

                x.Schedule(dateOfMatchday);
                _fromDate = _scheduledItems.MaxOrDefault(x => x.GetPeriod().End, x.Date);
            });
        }

        public DateTime GetFromDate() => _fromDate;

        public void Reset(DateTime fromDate, IEnumerable<IMatchesStage>? scheduledItems = null)
        {
            _scheduledItems.Set(scheduledItems?.SelectMany(x => x.GetAllMatches()) ?? []);
            _fromDate = fromDate;
        }
    }
}


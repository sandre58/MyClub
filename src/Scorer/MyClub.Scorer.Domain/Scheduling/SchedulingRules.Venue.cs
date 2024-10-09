// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Domain.Scheduling
{
    public enum UseRotationTime
    {
        Yes,

        No,

        YesOrOtherwiseNo
    }

    public class FirstAvailableStadiumRule : ValueObject, IAvailableVenueSchedulingRule
    {
        public FirstAvailableStadiumRule(UseRotationTime useRotationTime) => UseRotationTime = useRotationTime;

        public UseRotationTime UseRotationTime { get; }

        public AvailableStadiumResult? GetAvailableStadium(Match match, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
        {
            var availableStadiums = stadiums.Where(x => x.IsAvailable(scheduledMatches, match.GetPeriod(), UseRotationTime is UseRotationTime.Yes or UseRotationTime.YesOrOtherwiseNo)).ToList();
            if (availableStadiums.Count == 0 && UseRotationTime == UseRotationTime.YesOrOtherwiseNo)
                availableStadiums = stadiums.Where(x => x.IsAvailable(scheduledMatches, match.GetPeriod(), false)).ToList();

            if (availableStadiums.Count == 0) return null;

            var stadium = availableStadiums.Select(x => new { Stadium = x, EndOfLastMatch = GetEndOfLastMatch(x, scheduledMatches) }).OrderBy(x => x.EndOfLastMatch).FirstOrDefault()?.Stadium;

            return new AvailableStadiumResult(stadium?.Id, true);
        }

        private static DateTime GetEndOfLastMatch(Stadium stadium, IEnumerable<Match> scheduledMatches)
            => scheduledMatches.Where(x => x.State != MyClub.Domain.Enums.MatchState.Cancelled && x.Stadium is not null && x.Stadium == stadium).MaxOrDefault(x => x.GetPeriod().End);
    }

    public class HomeStadiumRule : ValueObject, IAvailableVenueSchedulingRule
    {
        public AvailableStadiumResult? GetAvailableStadium(Match match, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
            => match.Home?.Team.Stadium is not null ? new AvailableStadiumResult(match.Home.Team.Stadium.Id, false) : null;
    }

    public class AwayStadiumRule : ValueObject, IAvailableVenueSchedulingRule
    {
        public AvailableStadiumResult? GetAvailableStadium(Match match, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
            => match.Away?.Team.Stadium is not null ? new AvailableStadiumResult(match.Away.Team.Stadium.Id, false) : null;
    }

    public class NoStadiumRule : ValueObject, IAvailableVenueSchedulingRule
    {
        public AvailableStadiumResult? GetAvailableStadium(Match match, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums) => new AvailableStadiumResult(null, false);
    }

    public class StadiumOfDayRule : ValueObject, IAvailableVenueSchedulingRule
    {
        public StadiumOfDayRule(DayOfWeek day, Guid? stadiumId)
        {
            Day = day;
            StadiumId = stadiumId;
        }

        public DayOfWeek Day { get; }

        public Guid? StadiumId { get; }

        public AvailableStadiumResult? GetAvailableStadium(Match match, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
            => match.Date.DayOfWeek == Day
                ? new AvailableStadiumResult(StadiumId, true)
                : null;
    }

    public class StadiumOfDateRule : ValueObject, IAvailableVenueSchedulingRule
    {
        public StadiumOfDateRule(DateOnly date, Guid? stadiumId)
        {
            Date = date;
            StadiumId = stadiumId;
        }

        public DateOnly Date { get; }

        public Guid? StadiumId { get; }

        public AvailableStadiumResult? GetAvailableStadium(Match match, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
            => match.Date.ToDate() == Date
                ? new AvailableStadiumResult(StadiumId, true)
                : null;
    }

    public class StadiumOfDatesRangeRule : IAvailableVenueSchedulingRule
    {
        public StadiumOfDatesRangeRule(DateOnly startDate, DateOnly endDate, Guid? stadiumId)
        {
            StartDate = startDate;
            EndDate = endDate;
            StadiumId = stadiumId;
        }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public Guid? StadiumId { get; set; }

        public AvailableStadiumResult? GetAvailableStadium(Match match, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
            => new DatePeriod(StartDate, EndDate).Contains(match.Date.ToDate())
                ? new AvailableStadiumResult(StadiumId, true)
                : null;
    }
}


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

        public AvailableStadiumResult? GetAvailableStadium(Match match, int index, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
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
        public AvailableStadiumResult? GetAvailableStadium(Match match, int index, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
            => match.HomeTeam.Stadium is not null ? new AvailableStadiumResult(match.HomeTeam.Stadium.Id, false) : null;
    }

    public class AwayStadiumRule : ValueObject, IAvailableVenueSchedulingRule
    {
        public AvailableStadiumResult? GetAvailableStadium(Match match, int index, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
            => match.AwayTeam.Stadium is not null ? new AvailableStadiumResult(match.AwayTeam.Stadium.Id, false) : null;
    }

    public class NoStadiumRule : ValueObject, IAvailableVenueSchedulingRule
    {
        public AvailableStadiumResult? GetAvailableStadium(Match match, int index, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums) => new AvailableStadiumResult(null, false);
    }

    public class StadiumOfDayRule : ValueObject, IAvailableVenueSchedulingRule
    {
        public StadiumOfDayRule(DayOfWeek day, Guid? stadiumId, IEnumerable<StadiumOfMatchNumberRule>? matchExceptions = null)
        {
            Day = day;
            StadiumId = stadiumId;
            MatchExceptions = new List<StadiumOfMatchNumberRule>(matchExceptions ?? []).AsReadOnly();
        }

        public DayOfWeek Day { get; }

        public Guid? StadiumId { get; }

        public IReadOnlyCollection<StadiumOfMatchNumberRule> MatchExceptions { get; }

        public AvailableStadiumResult? GetAvailableStadium(Match match, int index, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
            => match.Date.DayOfWeek == Day
                ? MatchExceptions.Select(x => x.GetAvailableStadium(match, index, scheduledMatches, stadiums)).FirstOrDefault(x => x is not null) is AvailableStadiumResult result
                    ? result
                    : new AvailableStadiumResult(StadiumId, true)
                : (AvailableStadiumResult?)null;
    }

    public class StadiumOfDateRule : ValueObject, IAvailableVenueSchedulingRule
    {
        public StadiumOfDateRule(DateTime date, Guid? stadiumId, IEnumerable<StadiumOfMatchNumberRule>? matchExceptions = null)
        {
            Date = date;
            StadiumId = stadiumId;
            MatchExceptions = new List<StadiumOfMatchNumberRule>(matchExceptions ?? []).AsReadOnly();
        }

        public DateTime Date { get; }

        public Guid? StadiumId { get; }

        public IReadOnlyCollection<StadiumOfMatchNumberRule> MatchExceptions { get; }

        public AvailableStadiumResult? GetAvailableStadium(Match match, int index, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
            => match.Date.Date == Date
                ? MatchExceptions.Select(x => x.GetAvailableStadium(match, index, scheduledMatches, stadiums)).FirstOrDefault(x => x is not null) is AvailableStadiumResult result
                    ? result
                    : new AvailableStadiumResult(StadiumId, true)
                : (AvailableStadiumResult?)null;
    }

    public class StadiumOfMatchNumberRule : ValueObject, IAvailableVenueSchedulingRule
    {
        public StadiumOfMatchNumberRule(int matchNumber, Guid? stadiumId)
        {
            MatchNumber = matchNumber;
            StadiumId = stadiumId;
        }

        public int MatchNumber { get; }

        public Guid? StadiumId { get; }

        public AvailableStadiumResult? GetAvailableStadium(Match match, int index, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums) => new AvailableStadiumResult(MatchNumber == index + 1 ? StadiumId : null, true);
    }

    public class StadiumOfDatesRangeRule : IAvailableVenueSchedulingRule
    {
        public StadiumOfDatesRangeRule(DateTime startDate, DateTime endDate, Guid? stadiumId, IEnumerable<StadiumOfMatchNumberRule>? matchExceptions = null)
        {
            StartDate = startDate;
            EndDate = endDate;
            StadiumId = stadiumId;
            MatchExceptions = new List<StadiumOfMatchNumberRule>(matchExceptions ?? []).AsReadOnly();
        }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public Guid? StadiumId { get; set; }

        public IReadOnlyCollection<StadiumOfMatchNumberRule> MatchExceptions { get; } = [];

        public AvailableStadiumResult? GetAvailableStadium(Match match, int index, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums)
            => new Period(StartDate, EndDate).Contains(match.Date)
                ? MatchExceptions.Select(x => x.GetAvailableStadium(match, index, scheduledMatches, stadiums)).FirstOrDefault(x => x is not null) is AvailableStadiumResult result
                    ? result
                    : new AvailableStadiumResult(StadiumId, true)
                : (AvailableStadiumResult?)null;
    }
}


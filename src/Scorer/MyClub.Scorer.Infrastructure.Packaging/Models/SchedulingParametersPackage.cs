// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class SchedulingParametersPackage
    {
        [XmlAttribute("startDate")]
        public DateOnly StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateOnly EndDate { get; set; }

        [XmlAttribute("startTime")]
        public TimeOnly StartTime { get; set; }

        [XmlAttribute("rotationTime")]
        public TimeSpan RotationTime { get; set; }

        [XmlAttribute("restTime")]
        public TimeSpan RestTime { get; set; }

        [XmlAttribute("useHomeVenue")]
        public bool UseHomeVenue { get; set; }

        [XmlAttribute("asSoonAsPossible")]
        public bool AsSoonAsPossible { get; set; }

        [XmlAttribute("interval")]
        public TimeSpan Interval { get; set; }

        [XmlAttribute("scheduleByParent")]
        public bool ScheduleByParent { get; set; }

        [XmlArray("AsSoonAsPossibleSchedulingRules")]
        [XmlArrayItem("IncludeTimePeriodsRule", typeof(IncludeTimePeriodsRulePackage))]
        [XmlArrayItem("IncludeDaysOfWeekAsSoonAsPossibleRule", typeof(IncludeDaysOfWeekAsSoonAsPossibleRulePackage))]
        [XmlArrayItem("ExcludeDatesRangeAsSoonAsPossibleRule", typeof(ExcludeDatesRangeAsSoonAsPossibleRulePackage))]
        public List<AsSoonAsPossibleSchedulingRulePackage>? AsSoonAsPossibleRules { get; set; }

        [XmlArray("DateRules")]
        [XmlArrayItem("IncludeDaysOfWeekRule", typeof(IncludeDaysOfWeekRulePackage))]
        [XmlArrayItem("ExcludeDateRule", typeof(ExcludeDateRulePackage))]
        [XmlArrayItem("ExcludeDatesRangeRule", typeof(ExcludeDatesRangeRulePackage))]
        [XmlArrayItem("DateIntervalRule", typeof(DateIntervalRulePackage))]
        public List<DateSchedulingRulePackage>? DateRules { get; set; }

        [XmlArray("TimeRules")]
        [XmlArrayItem("TimeOfDayRule", typeof(TimeOfDayRulePackage))]
        [XmlArrayItem("TimeOfDateRule", typeof(TimeOfDateRulePackage))]
        [XmlArrayItem("TimeOfMatchNumberRule", typeof(TimeOfMatchNumberRulePackage))]
        [XmlArrayItem("TimeOfDateRangeRule", typeof(TimeOfDateRangeRulePackage))]
        public List<TimeSchedulingRulePackage>? TimeRules { get; set; }

        [XmlArray("VenueRules")]
        [XmlArrayItem("AwayStadiumRule", typeof(AwayStadiumRulePackage))]
        [XmlArrayItem("HomeStadiumRule", typeof(HomeStadiumRulePackage))]
        [XmlArrayItem("NoStadiumRule", typeof(NoStadiumRulePackage))]
        [XmlArrayItem("FirstAvailableStadiumRule", typeof(FirstAvailableStadiumRulePackage))]
        [XmlArrayItem("StadiumOfDayRule", typeof(StadiumOfDayRulePackage))]
        [XmlArrayItem("StadiumOfDateRule", typeof(StadiumOfDateRulePackage))]
        [XmlArrayItem("StadiumOfMatchNumberRule", typeof(StadiumOfMatchNumberRulePackage))]
        [XmlArrayItem("StadiumOfDateRangeRule", typeof(StadiumOfDateRangeRulePackage))]
        public List<VenueSchedulingRulePackage>? VenueRules { get; set; }
    }

    [XmlInclude(typeof(IncludeTimePeriodsRulePackage))]
    [XmlInclude(typeof(IncludeDaysOfWeekAsSoonAsPossibleRulePackage))]
    [XmlInclude(typeof(ExcludeDatesRangeAsSoonAsPossibleRulePackage))]
    public class AsSoonAsPossibleSchedulingRulePackage
    {
    }

    [XmlInclude(typeof(IncludeDaysOfWeekRulePackage))]
    [XmlInclude(typeof(ExcludeDateRulePackage))]
    [XmlInclude(typeof(ExcludeDatesRangeRulePackage))]
    [XmlInclude(typeof(DateIntervalRulePackage))]
    public class DateSchedulingRulePackage
    {
    }

    [XmlInclude(typeof(TimeOfDayRulePackage))]
    [XmlInclude(typeof(TimeOfDateRulePackage))]
    [XmlInclude(typeof(TimeOfMatchNumberRulePackage))]
    [XmlInclude(typeof(TimeOfDateRangeRulePackage))]
    public class TimeSchedulingRulePackage
    {
    }

    [XmlInclude(typeof(FirstAvailableStadiumRulePackage))]
    [XmlInclude(typeof(HomeStadiumRulePackage))]
    [XmlInclude(typeof(AwayStadiumRulePackage))]
    [XmlInclude(typeof(NoStadiumRulePackage))]
    [XmlInclude(typeof(StadiumOfDayRulePackage))]
    [XmlInclude(typeof(StadiumOfDateRulePackage))]
    [XmlInclude(typeof(StadiumOfMatchNumberRulePackage))]
    [XmlInclude(typeof(StadiumOfDateRangeRulePackage))]
    public class VenueSchedulingRulePackage
    {

    }

    public class IncludeTimePeriodsRulePackage : AsSoonAsPossibleSchedulingRulePackage
    {
        [XmlArray("TimePeriods")]
        [XmlArrayItem("TimePeriod", typeof(TimePeriodPackage))]
        public List<TimePeriodPackage>? TimePeriods { get; set; }
    }

    public class TimePeriodPackage
    {
        [XmlAttribute("startTime")]
        public TimeOnly StartTime { get; set; }

        [XmlAttribute("endTime")]
        public TimeOnly EndTime { get; set; }
    }

    public class IncludeDaysOfWeekAsSoonAsPossibleRulePackage : AsSoonAsPossibleSchedulingRulePackage
    {
        [XmlAttribute("daysOfWeek")]
        public string? DaysOfWeek { get; set; }
    }

    public class IncludeDaysOfWeekRulePackage : DateSchedulingRulePackage
    {
        [XmlAttribute("daysOfWeek")]
        public string? DaysOfWeek { get; set; }
    }

    public class ExcludeDatesRangeAsSoonAsPossibleRulePackage : AsSoonAsPossibleSchedulingRulePackage
    {
        [XmlAttribute("startDate")]
        public DateOnly StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateOnly EndDate { get; set; }

    }

    public class ExcludeDatesRangeRulePackage : DateSchedulingRulePackage
    {
        [XmlAttribute("startDate")]
        public DateOnly StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateOnly EndDate { get; set; }

    }

    public class ExcludeDateRulePackage : DateSchedulingRulePackage
    {
        [XmlAttribute("date")]
        public DateOnly Date { get; set; }
    }

    public class DateIntervalRulePackage : DateSchedulingRulePackage
    {
        [XmlAttribute("interval")]
        public TimeSpan Interval { get; set; }
    }

    public class TimeOfDayRulePackage : TimeSchedulingRulePackage
    {
        [XmlAttribute("day")]
        public DayOfWeek Day { get; set; }

        [XmlAttribute("time")]
        public TimeOnly Time { get; set; }

        [XmlArray("MatchExceptions")]
        [XmlArrayItem("MatchException", typeof(TimeOfMatchNumberRulePackage))]
        public List<TimeOfMatchNumberRulePackage>? MatchExceptions { get; set; }
    }

    public class TimeOfDateRulePackage : TimeSchedulingRulePackage
    {
        [XmlAttribute("date")]
        public DateOnly Date { get; set; }

        [XmlAttribute("time")]
        public TimeOnly Time { get; set; }

        [XmlArray("MatchExceptions")]
        [XmlArrayItem("MatchException", typeof(TimeOfMatchNumberRulePackage))]
        public List<TimeOfMatchNumberRulePackage>? MatchExceptions { get; set; }
    }

    public class TimeOfMatchNumberRulePackage : TimeSchedulingRulePackage
    {
        [XmlAttribute("matchNumber")]
        public int MatchNumber { get; set; }

        [XmlAttribute("time")]
        public TimeOnly Time { get; set; }
    }

    public class TimeOfDateRangeRulePackage : TimeSchedulingRulePackage
    {
        [XmlAttribute("startDate")]
        public DateOnly StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateOnly EndDate { get; set; }

        [XmlAttribute("time")]
        public TimeOnly Time { get; set; }

        [XmlArray("MatchExceptions")]
        [XmlArrayItem("MatchException", typeof(TimeOfMatchNumberRulePackage))]
        public List<TimeOfMatchNumberRulePackage>? MatchExceptions { get; set; }
    }

    public class StadiumOfDayRulePackage : VenueSchedulingRulePackage
    {
        [XmlAttribute("day")]
        public DayOfWeek Day { get; set; }

        [XmlElement("StadiumId", IsNullable = true)]
        public Guid? StadiumId { get; set; }

        [XmlIgnore]
        public bool StadiumIdSpecified => StadiumId.HasValue;

        [XmlArray("MatchExceptions")]
        [XmlArrayItem("MatchException", typeof(StadiumOfMatchNumberRulePackage))]
        public List<StadiumOfMatchNumberRulePackage>? MatchExceptions { get; set; }
    }

    public class StadiumOfDateRulePackage : VenueSchedulingRulePackage
    {
        [XmlAttribute("date")]
        public DateOnly Date { get; set; }

        [XmlElement("StadiumId", IsNullable = true)]
        public Guid? StadiumId { get; set; }

        [XmlIgnore]
        public bool StadiumIdSpecified => StadiumId.HasValue;

        [XmlArray("MatchExceptions")]
        [XmlArrayItem("MatchException", typeof(StadiumOfMatchNumberRulePackage))]
        public List<StadiumOfMatchNumberRulePackage>? MatchExceptions { get; set; }
    }

    public class StadiumOfMatchNumberRulePackage : VenueSchedulingRulePackage
    {
        [XmlAttribute("matchNumber")]
        public int MatchNumber { get; set; }


        [XmlElement("StadiumId", IsNullable = true)]
        public Guid? StadiumId { get; set; }

        [XmlIgnore]
        public bool StadiumIdSpecified => StadiumId.HasValue;
    }

    public class StadiumOfDateRangeRulePackage : VenueSchedulingRulePackage
    {
        [XmlAttribute("startDate")]
        public DateOnly StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateOnly EndDate { get; set; }

        [XmlElement("StadiumId", IsNullable = true)]
        public Guid? StadiumId { get; set; }

        [XmlIgnore]
        public bool StadiumIdSpecified => StadiumId.HasValue;

        [XmlArray("MatchExceptions")]
        [XmlArrayItem("MatchException", typeof(StadiumOfMatchNumberRulePackage))]
        public List<StadiumOfMatchNumberRulePackage>? MatchExceptions { get; set; }
    }

    public class HomeStadiumRulePackage : VenueSchedulingRulePackage
    {
    }

    public class AwayStadiumRulePackage : VenueSchedulingRulePackage
    {
    }

    public class NoStadiumRulePackage : VenueSchedulingRulePackage
    {
    }

    public class FirstAvailableStadiumRulePackage : VenueSchedulingRulePackage
    {
        public int UseRotationTime { get; set; }
    }
}

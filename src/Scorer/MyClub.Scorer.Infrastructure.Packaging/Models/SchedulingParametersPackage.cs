﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class SchedulingParametersPackage
    {
        [XmlAttribute("startDate")]
        public int StartDate { get; set; }

        [XmlAttribute("endDate")]
        public int EndDate { get; set; }

        [XmlAttribute("startTime")]
        public TimeSpan StartTime { get; set; }

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

        [XmlAttribute("scheduleByStage")]
        public bool ScheduleByStage { get; set; }

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
        [XmlArrayItem("TimeOfMatchNumberRule", typeof(TimeOfIndexRulePackage))]
        [XmlArrayItem("TimeOfDateRangeRule", typeof(TimeOfDateRangeRulePackage))]
        public List<TimeSchedulingRulePackage>? TimeRules { get; set; }

        [XmlArray("VenueRules")]
        [XmlArrayItem("AwayStadiumRule", typeof(AwayStadiumRulePackage))]
        [XmlArrayItem("HomeStadiumRule", typeof(HomeStadiumRulePackage))]
        [XmlArrayItem("NoStadiumRule", typeof(NoStadiumRulePackage))]
        [XmlArrayItem("FirstAvailableStadiumRule", typeof(FirstAvailableStadiumRulePackage))]
        [XmlArrayItem("StadiumOfDayRule", typeof(StadiumOfDayRulePackage))]
        [XmlArrayItem("StadiumOfDateRule", typeof(StadiumOfDateRulePackage))]
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
    [XmlInclude(typeof(TimeOfIndexRulePackage))]
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
        public TimeSpan StartTime { get; set; }

        [XmlAttribute("endTime")]
        public TimeSpan EndTime { get; set; }
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
        public int StartDate { get; set; }

        [XmlAttribute("endDate")]
        public int EndDate { get; set; }

    }

    public class ExcludeDatesRangeRulePackage : DateSchedulingRulePackage
    {
        [XmlAttribute("startDate")]
        public int StartDate { get; set; }

        [XmlAttribute("endDate")]
        public int EndDate { get; set; }

    }

    public class ExcludeDateRulePackage : DateSchedulingRulePackage
    {
        [XmlAttribute("date")]
        public int Date { get; set; }
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
        public TimeSpan Time { get; set; }

        [XmlArray("Exceptions")]
        [XmlArrayItem("Exception", typeof(TimeOfIndexRulePackage))]
        public List<TimeOfIndexRulePackage>? Exceptions { get; set; }
    }

    public class TimeOfDateRulePackage : TimeSchedulingRulePackage
    {
        [XmlAttribute("date")]
        public int Date { get; set; }

        [XmlAttribute("time")]
        public TimeSpan Time { get; set; }

        [XmlArray("Exceptions")]
        [XmlArrayItem("Exception", typeof(TimeOfIndexRulePackage))]
        public List<TimeOfIndexRulePackage>? Exceptions { get; set; }
    }

    public class TimeOfIndexRulePackage : TimeSchedulingRulePackage
    {
        [XmlAttribute("index")]
        public int Index { get; set; }

        [XmlAttribute("time")]
        public TimeSpan Time { get; set; }
    }

    public class TimeOfDateRangeRulePackage : TimeSchedulingRulePackage
    {
        [XmlAttribute("startDate")]
        public int StartDate { get; set; }

        [XmlAttribute("endDate")]
        public int EndDate { get; set; }

        [XmlAttribute("time")]
        public TimeSpan Time { get; set; }

        [XmlArray("Exceptions")]
        [XmlArrayItem("Exception", typeof(TimeOfIndexRulePackage))]
        public List<TimeOfIndexRulePackage>? Exceptions { get; set; }
    }

    public class StadiumOfDayRulePackage : VenueSchedulingRulePackage
    {
        [XmlAttribute("day")]
        public DayOfWeek Day { get; set; }

        [XmlElement("StadiumId", IsNullable = true)]
        public Guid? StadiumId { get; set; }

        [XmlIgnore]
        public bool StadiumIdSpecified => StadiumId.HasValue;
    }

    public class StadiumOfDateRulePackage : VenueSchedulingRulePackage
    {
        [XmlAttribute("date")]
        public int Date { get; set; }

        [XmlElement("StadiumId", IsNullable = true)]
        public Guid? StadiumId { get; set; }

        [XmlIgnore]
        public bool StadiumIdSpecified => StadiumId.HasValue;
    }

    public class StadiumOfDateRangeRulePackage : VenueSchedulingRulePackage
    {
        [XmlAttribute("startDate")]
        public int StartDate { get; set; }

        [XmlAttribute("endDate")]
        public int EndDate { get; set; }

        [XmlElement("StadiumId", IsNullable = true)]
        public Guid? StadiumId { get; set; }

        [XmlIgnore]
        public bool StadiumIdSpecified => StadiumId.HasValue;
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

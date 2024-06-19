// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlType("schedulingParameters")]
    public class SchedulingParametersPackage
    {
        [XmlAttribute("startDate")]
        public DateTime StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateTime EndDate { get; set; }

        [XmlAttribute("startTime")]
        public TimeSpan StartTime { get; set; }

        [XmlAttribute("rotationTime")]
        public TimeSpan RotationTime { get; set; }

        [XmlAttribute("restTime")]
        public TimeSpan RestTime { get; set; }

        [XmlAttribute("useTeamVenues")]
        public bool UseTeamVenues { get; set; }
    }
}

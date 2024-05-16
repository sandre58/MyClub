// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("trainingSession")]
    public class TrainingSessionPackage : AuditablePackage
    {
        [XmlElement("theme", IsNullable = true)]
        public string? Theme { get; set; }

        [XmlIgnore]
        public bool ThemeSpecified => !string.IsNullOrEmpty(Theme);

        [XmlElement("place", IsNullable = true)]
        public string? Place { get; set; }

        [XmlIgnore]
        public bool PlaceSpecified => !string.IsNullOrEmpty(Place);

        [XmlAttribute("startDate")]
        public DateTime StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateTime EndDate { get; set; }

        [XmlAttribute("isCancelled")]
        public bool IsCancelled { get; set; }

        [XmlAttribute("teamIds")]
        public string TeamIds { get; set; } = string.Empty;

        [XmlArray("attendances")]
        [XmlArrayItem("attendance", typeof(TrainingAttendancePackage))]
        public List<TrainingAttendancePackage>? Attendances { get; set; }

        [XmlAttribute("stages")]
        public string Stages { get; set; } = string.Empty;

        [XmlAttribute("technicalGoals")]
        public string TechnicalGoals { get; set; } = string.Empty;

        [XmlAttribute("tacticalGoals")]
        public string TacticalGoals { get; set; } = string.Empty;

        [XmlAttribute("physicalGoals")]
        public string PhysicalGoals { get; set; } = string.Empty;

        [XmlAttribute("mentalGoals")]
        public string MentalGoals { get; set; } = string.Empty;
    }
}

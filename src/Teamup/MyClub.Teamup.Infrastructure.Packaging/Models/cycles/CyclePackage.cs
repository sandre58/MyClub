// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("cycle")]
    public class CyclePackage : AuditablePackage
    {
        [XmlAttribute("label")]
        public string Label { get; set; } = string.Empty;

        [XmlAttribute("startDate")]
        public DateTime StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateTime EndDate { get; set; }

        [XmlElement("color", IsNullable = true)]
        public string? Color { get; set; }

        [XmlIgnore]
        public bool ColorSpecified => !string.IsNullOrEmpty(Color);

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

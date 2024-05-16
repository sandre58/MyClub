// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class InjuryPackage : AuditablePackage
    {
        [XmlAttribute("condition")]
        public string Condition { get; set; } = string.Empty;

        [XmlAttribute("severity")]
        public int Severity { get; set; }

        [XmlAttribute("type")]
        public int Type { get; set; }

        [XmlAttribute("category")]
        public int Category { get; set; }

        [XmlElement("description", IsNullable = true)]
        public string? Description { get; set; }

        [XmlIgnore]
        public bool DescriptionSpecified => Description is not null;

        [XmlElement("date")]
        public DateTime Date { get; set; }

        [XmlElement("endDate", IsNullable = true)]
        public DateTime? EndDate { get; set; }

        [XmlIgnore]
        public bool EndDateSpecified => EndDate.HasValue;
    }
}

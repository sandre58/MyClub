// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class SeasonCompetitionPackage : AuditablePackage
    {
        [XmlAttribute("seasonId")]
        public Guid SeasonId { get; set; }

        [XmlAttribute("startDate")]
        public DateTime StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateTime EndDate { get; set; }

        [XmlAttribute("teamIds")]
        public string TeamIds { get; set; } = string.Empty;

        [XmlAttribute("matchTime")]
        public TimeSpan MatchTime { get; set; }

        [XmlElement("matchFormat")]
        public MatchFormatPackage? MatchFormat { get; set; }
    }
}

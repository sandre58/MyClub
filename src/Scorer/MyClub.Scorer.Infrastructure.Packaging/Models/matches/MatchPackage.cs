// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class MatchPackage : AuditablePackage
    {
        [XmlElement("format")]
        public MatchFormatPackage? Format { get; set; }

        [XmlAttribute("date")]
        public DateTime OriginDate { get; set; }

        [XmlAttribute("state")]
        public int State { get; set; }

        [XmlElement("postponedDate", IsNullable = true)]
        public DateTime? PostponedDate { get; set; }

        [XmlIgnore]
        public bool PostponedDateSpecified => PostponedDate.HasValue;

        [XmlElement("home")]
        public MatchOpponentPackage? Home { get; set; }

        [XmlElement("away")]
        public MatchOpponentPackage? Away { get; set; }

        [XmlAttribute("neutralVenue")]
        public bool NeutralVenue { get; set; }

        [XmlElement("stadiumId", IsNullable = true)]
        public Guid? StadiumId { get; set; }

        [XmlIgnore]
        public bool StadiumIdSpecified => StadiumId.HasValue;

        [XmlAttribute("afterExtraTime")]
        public bool AfterExtraTime { get; set; }
    }
}

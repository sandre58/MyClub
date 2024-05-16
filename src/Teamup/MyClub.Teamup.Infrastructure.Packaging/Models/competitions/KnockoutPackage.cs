// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("knockout")]
    public class KnockoutPackage : RoundPackage
    {
        [XmlAttribute("date")]
        public DateTime OriginDate { get; set; }

        [XmlElement("postponedDate", IsNullable = true)]
        public DateTime? PostponedDate { get; set; }

        [XmlIgnore]
        public bool PostponedDateSpecified => PostponedDate.HasValue;

        [XmlAttribute("isPostponed")]
        public bool IsPostponed { get; set; }

        [XmlArray("matches")]
        [XmlArrayItem("match", typeof(MatchPackage))]
        public List<MatchPackage>? Matches { get; set; }
    }
}

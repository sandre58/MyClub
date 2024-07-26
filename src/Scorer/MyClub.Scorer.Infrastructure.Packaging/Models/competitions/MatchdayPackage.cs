// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class MatchdayPackage : NamePackage
    {
        [XmlAttribute("date")]
        public DateTime OriginDate { get; set; }

        [XmlElement("PostponedDate", IsNullable = true)]
        public DateTime? PostponedDate { get; set; }

        [XmlIgnore]
        public bool PostponedDateSpecified => PostponedDate.HasValue;

        [XmlAttribute("isPostponed")]
        public bool IsPostponed { get; set; }

        [XmlArray("Matches")]
        [XmlArrayItem("Match", typeof(MatchPackage))]
        public List<MatchPackage>? Matches { get; set; }
    }
}

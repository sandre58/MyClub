// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("groupStage")]
    public class GroupStagePackage : RoundPackage
    {
        [XmlAttribute("startDate")]
        public DateTime StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateTime EndDate { get; set; }

        [XmlElement("rankingRules")]
        public RankingRulesPackage? RankingRules { get; set; }

        [XmlArray("groups")]
        [XmlArrayItem("group", typeof(GroupPackage))]
        public List<GroupPackage>? Groups { get; set; }

        [XmlArray("matchdays")]
        [XmlArrayItem("matchday", typeof(MatchdayPackage))]
        public List<MatchdayPackage>? Matchdays { get; set; }

    }
}

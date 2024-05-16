// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlType("league")]
    public class LeaguePackage : CompetitionPackage
    {
        [XmlArray("matchdays")]
        [XmlArrayItem("matchday", typeof(MatchdayPackage))]
        public List<MatchdayPackage>? Matchdays { get; set; }

        [XmlElement("matchFormat")]
        public MatchFormatPackage? MatchFormat { get; set; }

        [XmlElement("rankingRules")]
        public RankingRulesPackage? RankingRules { get; set; }

        [XmlArray("penalties")]
        [XmlArrayItem("penalty", typeof(PenaltyPackage))]
        public List<PenaltyPackage>? Penalties { get; set; }

        [XmlArray("labels")]
        [XmlArrayItem("label", typeof(RankLabelPackage))]
        public List<RankLabelPackage>? Labels { get; set; }
    }
}

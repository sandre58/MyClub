// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class LeaguePackage : CompetitionPackage
    {
        [XmlArray("Matchdays")]
        [XmlArrayItem("Matchday", typeof(MatchdayPackage))]
        public List<MatchdayPackage>? Matchdays { get; set; }

        [XmlElement("MatchFormat")]
        public MatchFormatPackage? MatchFormat { get; set; }

        [XmlElement("MatchRules")]
        public MatchRulesPackage? MatchRules { get; set; }

        [XmlElement("RankingRules")]
        public RankingRulesPackage? RankingRules { get; set; }

        [XmlArray("Penalties")]
        [XmlArrayItem("Penalty", typeof(PenaltyPackage))]
        public List<PenaltyPackage>? Penalties { get; set; }

        [XmlArray("Labels")]
        [XmlArrayItem("Label", typeof(RankLabelPackage))]
        public List<RankLabelPackage>? Labels { get; set; }
    }
}

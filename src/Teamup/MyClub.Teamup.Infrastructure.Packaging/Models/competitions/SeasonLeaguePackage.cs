// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class SeasonLeaguePackage : SeasonCompetitionPackage
    {
        [XmlElement("rankingRules")]
        public RankingRulesPackage? RankingRules { get; set; }

        [XmlArray("penalties")]
        [XmlArrayItem("penalty", typeof(PenaltyPackage))]
        public List<PenaltyPackage>? Penalties { get; set; }

        [XmlArray("matchdays")]
        [XmlArrayItem("matchday", typeof(MatchdayPackage))]
        public List<MatchdayPackage>? Matchdays { get; set; }
    }
}

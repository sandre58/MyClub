// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("league")]
    public class LeaguePackage : CompetitionPackage
    {
        [XmlElement("rankingRules")]
        public RankingRulesPackage? RankingRules { get; set; }

        [XmlArray("seasons")]
        [XmlArrayItem("season", typeof(SeasonLeaguePackage))]
        public List<SeasonLeaguePackage>? Seasons { get; set; }
    }
}

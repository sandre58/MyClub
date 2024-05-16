// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class RankingRulesPackage
    {
        [XmlAttribute("pointsByGamesWon")]
        public int PointsByGamesWon { get; set; }

        [XmlAttribute("pointsByGamesDrawn")]
        public int PointsByGamesDrawn { get; set; }

        [XmlAttribute("pointsByGamesLost")]
        public int PointsByGamesLost { get; set; }

        [XmlAttribute("sortingColumns")]
        public string SortingColumns { get; set; } = string.Empty;

        [XmlArray("labels")]
        [XmlArrayItem("label", typeof(RankLabelPackage))]
        public List<RankLabelPackage>? Labels { get; set; }
    }
}

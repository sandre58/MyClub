// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class RankingRulesPackage
    {
        [XmlArray("points")]
        [XmlArrayItem("pointsNumber", typeof(PointsNumberByResultPackage))]
        public List<PointsNumberByResultPackage>? Points { get; set; }

        [XmlElement("comparers")]
        public string? Comparers { get; set; }

        [XmlElement("commputers")]
        public string? Computers { get; set; }
    }
}

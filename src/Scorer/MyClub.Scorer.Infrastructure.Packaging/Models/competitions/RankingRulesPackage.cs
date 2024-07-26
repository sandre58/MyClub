// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class RankingRulesPackage
    {
        [XmlArray("Points")]
        [XmlArrayItem("PointsNumber", typeof(PointsNumberByResultPackage))]
        public List<PointsNumberByResultPackage>? Points { get; set; }

        [XmlElement("Comparers")]
        public string? Comparers { get; set; }

        [XmlElement("Computers")]
        public string? Computers { get; set; }
    }
}

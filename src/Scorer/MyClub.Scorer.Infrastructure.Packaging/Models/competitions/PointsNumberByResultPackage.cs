// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class PointsNumberByResultPackage
    {
        [XmlAttribute("result")]
        public int Result { get; set; }

        [XmlAttribute("points")]
        public int Points { get; set; }
    }
}

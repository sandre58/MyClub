// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class TacticPositionPackage : EntityPackage
    {
        [XmlAttribute("position")]
        public int Position { get; set; }

        [XmlElement("number", IsNullable = true)]
        public int? Number { get; set; }

        [XmlIgnore]
        public bool NumberSpecified => Number.HasValue;

        [XmlAttribute("offsetX")]
        public double OffsetX { get; set; }

        [XmlAttribute("offsetY")]
        public double OffsetY { get; set; }

        [XmlAttribute("instructions")]
        public string Instructions { get; set; } = string.Empty;
    }
}

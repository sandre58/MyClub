// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class CardPackage : MatchEventPackage
    {
        [XmlAttribute("color")]
        public int Color { get; set; }

        [XmlAttribute("infraction")]
        public int Infraction { get; set; }

        [XmlElement("Description", IsNullable = true)]
        public string? Description { get; set; }

        [XmlIgnore]
        public bool DescriptionSpecified => !string.IsNullOrEmpty(Description);

        [XmlElement("PlayerId", IsNullable = true)]
        public Guid? PlayerId { get; set; }

        [XmlIgnore]
        public bool PlayerIdSpecified => PlayerId.HasValue;
    }
}

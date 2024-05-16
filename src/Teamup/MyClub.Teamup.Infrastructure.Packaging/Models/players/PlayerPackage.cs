// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class PlayerPackage : PersonPackage
    {
        [XmlElement("category", IsNullable = true)]
        public int? Category { get; set; }

        [XmlIgnore]
        public bool CategorySpecified => Category is not null;

        [XmlAttribute("laterality")]
        public int Laterality { get; set; }

        [XmlElement("height", IsNullable = true)]
        public int? Height { get; set; }

        [XmlIgnore]
        public bool HeightSpecified => Height.HasValue;

        [XmlElement("weight", IsNullable = true)]
        public int? Weight { get; set; }

        [XmlIgnore]
        public bool WeightSizeSpecified => Weight.HasValue;

        [XmlArray("injuries")]
        [XmlArrayItem("injury", typeof(InjuryPackage))]
        public List<InjuryPackage>? Injuries { get; set; }

        [XmlArray("positions")]
        [XmlArrayItem("position", typeof(RatedPositionPackage))]
        public List<RatedPositionPackage>? Positions { get; set; }

        [XmlArray("absences")]
        [XmlArrayItem("absence", typeof(AbsencePackage))]
        public List<AbsencePackage>? Absences { get; set; }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class RankLabelPackage
    {
        [XmlAttribute("startRank")]
        public int StartRank { get; set; }

        [XmlAttribute("endRank")]
        public int EndRank { get; set; }

        [XmlElement("Color", IsNullable = true)]
        public string? Color { get; set; }

        [XmlIgnore]
        public bool ColorSpecified => !string.IsNullOrEmpty(Color);

        [XmlAttribute("shortName")]
        public string ShortName { get; set; } = string.Empty;

        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;

        [XmlElement("Description", IsNullable = true)]
        public string? Description { get; set; }

        [XmlIgnore]
        public bool DescriptionSpecified => !string.IsNullOrEmpty(Description);
    }
}

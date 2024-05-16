// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace MyClub.CrossCutting.Packaging.Models
{
    public class LabelPackage : AuditablePackage
    {
        [XmlAttribute("label")]
        public string Label { get; set; } = string.Empty;

        [XmlElement("description", IsNullable = true)]
        public string? Description { get; set; }

        [XmlIgnore]
        public bool DescriptionSpecified => Description is not null;

        [XmlAttribute("code")]
        public string Code { get; set; } = string.Empty;

        [XmlElement("order", IsNullable = true)]
        public int? Order { get; set; }

        [XmlIgnore]
        public bool OrderSpecified => Order.HasValue;

    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class ContactPackage
    {
        [XmlElement("label", IsNullable = true)]
        public string? Label { get; set; }

        [XmlIgnore]
        public bool LabelSpecified => Label is not null;

        [XmlAttribute("default")]
        public bool Default { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; } = string.Empty;
    }
}

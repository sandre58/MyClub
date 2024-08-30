// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlRoot("Metadata", Namespace = CrossCutting.Packaging.XmlConstants.MyClubNamespace)]
    public class MetadataPackage
    {
        [XmlElement("Guid")]
        public Guid Id { get; set; }

        [XmlElement("Type")]
        public int Type { get; set; }

        [XmlElement("Name")]
        public string Name { get; set; } = string.Empty;

        [XmlIgnore]
        public byte[]? Image { get; set; }

        public ProjectPreferencesPackage? Preferences { get; set; }

        [XmlElement("CreatedAt", IsNullable = true)]
        public DateTime? CreatedAt { get; set; }

        [XmlIgnore]
        public bool CreatedAtSpecified => CreatedAt.HasValue;

        [XmlElement("CreatedBy", IsNullable = true)]
        public string? CreatedBy { get; set; }

        [XmlIgnore]
        public bool CreatedBySpecified => CreatedBy is not null;

        [XmlElement("ModifiedAt", IsNullable = true)]
        public DateTime? ModifiedAt { get; set; }

        [XmlIgnore]
        public bool ModifiedAtSpecified => ModifiedAt.HasValue;

        [XmlElement("ModifiedBy", IsNullable = true)]
        public string? ModifiedBy { get; set; }

        [XmlIgnore]
        public bool ModifiedBySpecified => ModifiedBy is not null;
    }
}

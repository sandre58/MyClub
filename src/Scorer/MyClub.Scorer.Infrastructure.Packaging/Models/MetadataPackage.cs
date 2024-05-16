// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlRoot("metadata", Namespace = CrossCutting.Packaging.XmlConstants.MyClubNamespace)]
    public class MetadataPackage
    {
        [XmlElement("guid")]
        public Guid Id { get; set; }

        [XmlElement("type")]
        public int Type { get; set; }

        [XmlElement("name")]
        public string Name { get; set; } = string.Empty;

        [XmlIgnore]
        public byte[]? Image { get; set; }

        [XmlElement("startDate")]
        public DateTime StartDate { get; set; }

        [XmlElement("endDate")]
        public DateTime EndDate { get; set; }

        public ProjectParametersPackage? Parameters { get; set; }

        [XmlElement("createdAt", IsNullable = true)]
        public DateTime? CreatedAt { get; set; }

        [XmlIgnore]
        public bool CreatedAtSpecified => CreatedAt.HasValue;

        [XmlElement("createdBy", IsNullable = true)]
        public string? CreatedBy { get; set; }

        [XmlIgnore]
        public bool CreatedBySpecified => CreatedBy is not null;

        [XmlElement("modifiedAt", IsNullable = true)]
        public DateTime? ModifiedAt { get; set; }

        [XmlIgnore]
        public bool ModifiedAtSpecified => ModifiedAt.HasValue;

        [XmlElement("modifiedBy", IsNullable = true)]
        public string? ModifiedBy { get; set; }

        [XmlIgnore]
        public bool ModifiedBySpecified => ModifiedBy is not null;
    }
}

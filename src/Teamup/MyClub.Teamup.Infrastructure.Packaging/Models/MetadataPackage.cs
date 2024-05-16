// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("metadata", Namespace = CrossCutting.Packaging.XmlConstants.MyClubNamespace)]
    public class MetadataPackage
    {
        [XmlElement("guid")]
        public Guid Id { get; set; }

        [XmlElement("clubId")]
        public Guid ClubId { get; set; }

        [XmlElement("name")]
        public string Name { get; set; } = string.Empty;

        [XmlIgnore]
        public byte[]? Image { get; set; }

        [XmlElement("Color")]
        public string Color { get; set; } = string.Empty;

        [XmlElement("category")]
        public int Category { get; set; }

        [XmlElement("seasonId")]
        public Guid SeasonId { get; set; }

        [XmlElement("teamId")]
        public Guid? TeamId { get; set; }

        [XmlIgnore]
        public bool TeamIdSpecified => TeamId.HasValue;

        [XmlElement("preferences")]
        public ProjectPreferencesPackage? Preferences { get; set; }

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

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.CrossCutting.Packaging.Models
{
    public class AuditablePackage : EntityPackage
    {
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

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("player")]
    public class SquadPlayerPackage : AuditablePackage
    {
        [XmlElement("playerId")]
        public Guid PlayerId { get; set; }

        [XmlElement("teamId", IsNullable = true)]
        public Guid? TeamId { get; set; }

        [XmlIgnore]
        public bool TeamIdSpecified => TeamId.HasValue;

        [XmlElement("category", IsNullable = true)]
        public int? Category { get; set; }

        [XmlIgnore]
        public bool CategorySpecified => Category.HasValue;

        [XmlAttribute("licenseState")]
        public int LicenseState { get; set; }

        [XmlAttribute("isMutation")]
        public bool IsMutation { get; set; }

        [XmlElement("fromDate", IsNullable = true)]
        public DateTime? FromDate { get; set; }

        [XmlIgnore]
        public bool FromDateSpecified => FromDate.HasValue;

        [XmlElement("number", IsNullable = true)]
        public int? Number { get; set; }

        [XmlIgnore]
        public bool NumberSpecified => Number.HasValue;

        [XmlElement("shoesSize", IsNullable = true)]
        public int? ShoesSize { get; set; }

        [XmlIgnore]
        public bool ShoesSizeSpecified => ShoesSize.HasValue;

        [XmlAttribute("size")]
        public string? Size { get; set; }

        [XmlIgnore]
        public bool SizeSpecified => Size is not null;

        [XmlArray("positions")]
        [XmlArrayItem("position", typeof(RatedPositionPackage))]
        public List<RatedPositionPackage>? Positions { get; set; }
    }
}

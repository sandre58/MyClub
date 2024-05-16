// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class PenaltyShootoutPackage : EntityPackage
    {
        [XmlElement("takerId", IsNullable = true)]
        public Guid? TakerId { get; set; }

        [XmlIgnore]
        public bool TakerIdSpecified => TakerId.HasValue;

        [XmlAttribute("result")]
        public int Result { get; set; }
    }
}

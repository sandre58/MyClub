// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class GroupPackage : NamePackage
    {
        [XmlAttribute("teamIds")]
        public string TeamIds { get; set; } = string.Empty;

        [XmlArray("penalties")]
        [XmlArrayItem("penalty", typeof(PenaltyPackage))]
        public List<PenaltyPackage>? Penalties { get; set; }

        [XmlAttribute("order")]
        public int Order { get; set; }
    }
}

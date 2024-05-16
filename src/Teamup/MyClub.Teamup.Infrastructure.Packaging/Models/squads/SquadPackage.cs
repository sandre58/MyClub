// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("squad")]
    public class SquadPackage : LabelPackage
    {
        [XmlElement("clubId")]
        public Guid ClubId { get; set; }

        [XmlElement("seasonId")]
        public Guid SeasonId { get; set; }

        [XmlElement("category")]
        public int Category { get; set; }

        [XmlArray("players")]
        [XmlArrayItem("player", typeof(SquadPlayerPackage))]
        public List<SquadPlayerPackage>? Players { get; set; }
    }
}

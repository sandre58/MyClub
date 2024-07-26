// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlType("Team")]
    public class TeamPackage : NamePackage
    {
        [XmlIgnore]
        public byte[]? Logo { get; set; }

        [XmlElement("AwayColor", IsNullable = true)]
        public string? AwayColor { get; set; }

        [XmlIgnore]
        public bool AwayColorSpecified => !string.IsNullOrEmpty(AwayColor);

        [XmlElement("HomeColor", IsNullable = true)]
        public string? HomeColor { get; set; }

        [XmlIgnore]
        public bool HomeColorSpecified => !string.IsNullOrEmpty(HomeColor);

        [XmlElement("StadiumId", IsNullable = true)]
        public Guid? StadiumId { get; set; }

        [XmlIgnore]
        public bool StadiumIdSpecified => StadiumId.HasValue;

        [XmlElement("Country", IsNullable = true)]
        public int? Country { get; set; }

        [XmlIgnore]
        public bool CountrySpecified => Country is not null;

        [XmlArray("Players")]
        [XmlArrayItem("Player", typeof(PlayerPackage))]
        public List<PlayerPackage>? Players { get; set; }

        [XmlArray("Staff")]
        [XmlArrayItem("Manager", typeof(ManagerPackage))]
        public List<ManagerPackage>? Staff { get; set; }
    }
}

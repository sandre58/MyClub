// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("team")]
    public class TeamPackage : NamePackage
    {
        [XmlElement("category")]
        public int Category { get; set; }

        [XmlElement("awayColor", IsNullable = true)]
        public string? AwayColor { get; set; }

        [XmlIgnore]
        public bool AwayColorSpecified => !string.IsNullOrEmpty(AwayColor);

        [XmlElement("homeColor", IsNullable = true)]
        public string? HomeColor { get; set; }

        [XmlIgnore]
        public bool HomeColorSpecified => !string.IsNullOrEmpty(HomeColor);

        [XmlElement("stadiumId", IsNullable = true)]
        public Guid? StadiumId { get; set; }

        [XmlIgnore]
        public bool StadiumIdSpecified => StadiumId.HasValue;

        [XmlElement("country", IsNullable = true)]
        public int? Country { get; set; }

        [XmlIgnore]
        public bool CountrySpecified => Country is not null;

        [XmlElement("order", IsNullable = true)]
        public int? Order { get; set; }

        [XmlIgnore]
        public bool OrderSpecified => Order.HasValue;
    }
}

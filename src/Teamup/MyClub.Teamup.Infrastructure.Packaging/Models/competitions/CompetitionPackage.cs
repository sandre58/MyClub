// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class CompetitionPackage : NamePackage
    {
        [XmlElement("category")]
        public int Category { get; set; }

        [XmlIgnore]
        public byte[]? Logo { get; set; }

        [XmlAttribute("matchTime")]
        public TimeSpan MatchTime { get; set; }

        [XmlElement("matchFormat")]
        public MatchFormatPackage? MatchFormat { get; set; }
    }
}

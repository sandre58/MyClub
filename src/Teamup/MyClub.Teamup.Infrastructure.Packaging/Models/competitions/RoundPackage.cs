// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class RoundPackage : NamePackage
    {
        [XmlElement("matchFormat")]
        public MatchFormatPackage? MatchFormat { get; set; }

        [XmlAttribute("matchTime")]
        public TimeSpan MatchTime { get; set; }

        [XmlAttribute("teamIds")]
        public string TeamIds { get; set; } = string.Empty;
    }
}

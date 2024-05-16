// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class RatedPositionPackage : EntityPackage
    {
        [XmlAttribute("position")]
        public int Position { get; set; }

        [XmlAttribute("rating")]
        public int Rating { get; set; }

        [XmlAttribute("isNatural")]
        public bool IsNatural { get; set; }

    }
}

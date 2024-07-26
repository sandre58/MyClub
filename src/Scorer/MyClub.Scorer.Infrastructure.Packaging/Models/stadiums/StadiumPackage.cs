// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlType("Stadium")]
    public class StadiumPackage : EntityPackage
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;

        [XmlElement("Address", IsNullable = true)]
        public AddressPackage? Address { get; set; }

        [XmlAttribute("ground")]
        public int Ground { get; set; }
    }
}

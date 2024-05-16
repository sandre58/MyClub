// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace MyClub.CrossCutting.Packaging.Models
{
    public class NamePackage : AuditablePackage
    {
        [XmlAttribute("shortName")]
        public string ShortName { get; set; } = string.Empty;

        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;
    }
}

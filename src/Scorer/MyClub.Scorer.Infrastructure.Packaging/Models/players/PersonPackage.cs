// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class PersonPackage : AuditablePackage
    {
        [XmlAttribute("lastName")]
        public string LastName { get; set; } = string.Empty;

        [XmlAttribute("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [XmlElement("country", IsNullable = true)]
        public int? Country { get; set; }

        [XmlIgnore]
        public bool CountrySpecified => Country is not null;

        [XmlIgnore]
        public byte[]? Photo { get; set; }

        [XmlAttribute("gender")]
        public int Gender { get; set; }

        [XmlAttribute("licenseNumber")]
        public string? LicenseNumber { get; set; }

        [XmlIgnore]
        public bool LicenseNumberSpecified => LicenseNumber is not null;

        [XmlAttribute("email")]
        public string? Email { get; set; }

        [XmlIgnore]
        public bool EmailSpecified => Email is not null;
    }
}

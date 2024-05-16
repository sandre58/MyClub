// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class PersonPackage : AuditablePackage
    {
        [XmlAttribute("lastName")]
        public string LastName { get; set; } = string.Empty;

        [XmlAttribute("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [XmlElement("birthdate", IsNullable = true)]
        public DateTime? Birthdate { get; set; }

        [XmlIgnore]
        public bool BirthdateSpecified => Birthdate.HasValue;

        [XmlAttribute("placeOfBirth")]
        public string? PlaceOfBirth { get; set; }

        [XmlIgnore]
        public bool PlaceOfBirthSpecified => PlaceOfBirth is not null;

        [XmlElement("country", IsNullable = true)]
        public int? Country { get; set; }

        [XmlIgnore]
        public bool CountrySpecified => Country is not null;

        [XmlElement("address", IsNullable = true)]
        public AddressPackage? Address { get; set; }

        [XmlIgnore]
        public bool AddressSpecified => Address is not null;

        [XmlIgnore]
        public byte[]? Photo { get; set; }

        [XmlAttribute("gender")]
        public int Gender { get; set; }

        [XmlAttribute("licenseNumber")]
        public string? LicenseNumber { get; set; }

        [XmlIgnore]
        public bool LicenseNumberSpecified => LicenseNumber is not null;

        [XmlAttribute("description")]
        public string? Description { get; set; }

        [XmlIgnore]
        public bool DescriptionSpecified => Description is not null;

        [XmlArray("phones")]
        [XmlArrayItem("phone", typeof(ContactPackage))]
        public List<ContactPackage>? Phones { get; set; }

        [XmlArray("emails")]
        [XmlArrayItem("email", typeof(ContactPackage))]
        public List<ContactPackage>? Emails { get; set; }
    }
}

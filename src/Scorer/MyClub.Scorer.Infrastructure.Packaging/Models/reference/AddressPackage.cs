// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlType("address")]
    public class AddressPackage
    {
        [XmlAttribute("street")]
        public string? Street { get; set; }

        [XmlIgnore]
        public bool StreetSpecified => Street is not null;

        [XmlAttribute("postalCode")]
        public string? PostalCode { get; set; }

        [XmlIgnore]
        public bool PostalCodeSpecified => PostalCode is not null;

        [XmlAttribute("city")]
        public string? City { get; set; }

        [XmlIgnore]
        public bool CitySpecified => City is not null;

        [XmlElement("country", IsNullable = true)]
        public int? Country { get; set; }

        [XmlIgnore]
        public bool CountrySpecified => Country is not null;

        [XmlElement("latitude", IsNullable = true)]
        public double? Latitude { get; set; }

        [XmlIgnore]
        public bool LatitudeSpecified => Latitude is not null;

        [XmlElement("longitude", IsNullable = true)]
        public double? Longitude { get; set; }

        [XmlIgnore]
        public bool LongitudeSpecified => Longitude is not null;
    }
}

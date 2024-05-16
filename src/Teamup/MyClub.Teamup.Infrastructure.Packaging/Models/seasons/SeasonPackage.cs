// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("season")]
    public class SeasonPackage : LabelPackage
    {
        [XmlAttribute("startDate")]
        public DateTime StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateTime EndDate { get; set; }
    }
}

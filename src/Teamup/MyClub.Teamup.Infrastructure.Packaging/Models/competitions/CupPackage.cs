// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("cup")]
    public class CupPackage : CompetitionPackage
    {
        [XmlArray("seasons")]
        [XmlArrayItem("season", typeof(CupSeasonPackage))]
        public List<CupSeasonPackage>? Seasons { get; set; }
    }
}

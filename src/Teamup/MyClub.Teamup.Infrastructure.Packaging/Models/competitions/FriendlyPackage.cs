// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("friendly")]
    public class FriendlyPackage : CompetitionPackage
    {
        [XmlArray("seasons")]
        [XmlArrayItem("season", typeof(FriendlySeasonPackage))]
        public List<FriendlySeasonPackage>? Seasons { get; set; }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class FriendlySeasonPackage : SeasonCompetitionPackage
    {
        [XmlArray("matches")]
        [XmlArrayItem("match", typeof(MatchPackage))]
        public List<MatchPackage>? Matches { get; set; }
    }
}

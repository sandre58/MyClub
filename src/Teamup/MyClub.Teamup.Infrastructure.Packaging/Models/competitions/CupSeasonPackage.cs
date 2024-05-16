// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class CupSeasonPackage : SeasonCompetitionPackage
    {
        [XmlArray("rounds")]
        [XmlArrayItem("knockout", typeof(KnockoutPackage))]
        [XmlArrayItem("groupStage", typeof(GroupStagePackage))]
        public List<RoundPackage>? Rounds { get; set; }
    }
}

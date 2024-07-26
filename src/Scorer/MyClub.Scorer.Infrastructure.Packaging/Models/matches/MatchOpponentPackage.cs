// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class MatchOpponentPackage
    {
        [XmlAttribute("teamId")]
        public Guid TeamId { get; set; }

        [XmlAttribute("isWithdrawn")]
        public bool IsWithdrawn { get; set; }

        [XmlArray("Goals")]
        [XmlArrayItem("Goal", typeof(GoalPackage))]
        public List<GoalPackage>? Goals { get; set; }

        [XmlArray("Cards")]
        [XmlArrayItem("Card", typeof(CardPackage))]
        public List<CardPackage>? Cards { get; set; }

        [XmlArray("Shootout")]
        [XmlArrayItem("PenaltyShootouts", typeof(PenaltyShootoutPackage))]
        public List<PenaltyShootoutPackage>? Shootout { get; set; }
    }
}

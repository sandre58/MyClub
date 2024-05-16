// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class MatchOpponentPackage
    {
        [XmlAttribute("teamId")]
        public Guid TeamId { get; set; }

        [XmlAttribute("isWithdrawn")]
        public bool IsWithdrawn { get; set; }

        [XmlAttribute("penaltyPoints")]
        public int PenaltyPoints { get; set; }

        [XmlArray("goals")]
        [XmlArrayItem("goal", typeof(GoalPackage))]
        public List<GoalPackage>? Goals { get; set; }

        [XmlArray("cards")]
        [XmlArrayItem("card", typeof(CardPackage))]
        public List<CardPackage>? Cards { get; set; }

        [XmlArray("shootout")]
        [XmlArrayItem("penaltyShootouts", typeof(PenaltyShootoutPackage))]
        public List<PenaltyShootoutPackage>? Shootout { get; set; }
    }
}

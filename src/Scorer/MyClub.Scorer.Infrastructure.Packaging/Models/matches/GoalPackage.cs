// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class GoalPackage : MatchEventPackage
    {
        [XmlAttribute("type")]
        public int Type { get; set; }

        [XmlElement("ScorerId", IsNullable = true)]
        public Guid? ScorerId { get; set; }

        [XmlIgnore]
        public bool ScorerIdSpecified => ScorerId.HasValue;

        [XmlElement("AssistId", IsNullable = true)]
        public Guid? AssistId { get; set; }

        [XmlIgnore]
        public bool AssistIdSpecified => AssistId.HasValue;
    }
}

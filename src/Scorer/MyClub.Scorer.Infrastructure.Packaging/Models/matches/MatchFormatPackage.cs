﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class MatchFormatPackage
    {
        [XmlElement("RegulationTime")]
        public HalfFormatPackage? RegulationTime { get; set; }

        [XmlElement("ExtraTime", IsNullable = true)]
        public HalfFormatPackage? ExtraTime { get; set; }

        [XmlIgnore]
        public bool ExtraTimeSpecified => ExtraTime is not null;

        [XmlElement("NumberOfPenaltyShootouts", IsNullable = true)]
        public int? NumberOfPenaltyShootouts { get; set; }

        [XmlIgnore]
        public bool NumberOfPenaltyShootoutsSpecified => NumberOfPenaltyShootouts.HasValue;
    }
}

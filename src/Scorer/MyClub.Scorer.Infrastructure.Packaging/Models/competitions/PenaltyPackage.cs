// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class PenaltyPackage
    {
        [XmlAttribute("teamId")]
        public Guid TeamId { get; set; }

        [XmlAttribute("penalty")]
        public int Penalty { get; set; }
    }
}

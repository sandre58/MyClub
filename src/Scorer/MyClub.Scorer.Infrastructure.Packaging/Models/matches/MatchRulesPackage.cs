// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class MatchRulesPackage
    {
        [XmlElement("AllowedCards")]
        public string? AllowedCards { get; set; }
    }
}

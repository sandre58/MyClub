// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public abstract class MatchEventPackage : EntityPackage
    {
        [XmlElement("minute", IsNullable = true)]
        public int? Minute { get; set; }

        [XmlIgnore]
        public bool MinuteSpecified => Minute.HasValue;

    }
}

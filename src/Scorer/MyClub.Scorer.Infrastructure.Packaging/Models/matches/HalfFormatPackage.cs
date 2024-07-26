// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class HalfFormatPackage
    {
        [XmlAttribute("number")]
        public int Number { get; set; }

        [XmlAttribute("duration")]
        public TimeSpan Duration { get; set; }

        [XmlElement("HalfTimeDuration", IsNullable = true)]
        public TimeSpan? HalfTimeDuration { get; set; }

        [XmlIgnore]
        public bool HalfTimeDurationSpecified => HalfTimeDuration.HasValue;

    }
}

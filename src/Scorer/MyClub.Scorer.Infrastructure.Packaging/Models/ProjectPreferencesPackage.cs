// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.


// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class ProjectPreferencesPackage
    {
        [XmlElement("TreatNoStadiumAsWarning")]
        public bool TreatNoStadiumAsWarning { get; set; }

        [XmlElement("PeriodForPreviousMatches")]
        public TimeSpan PeriodForPreviousMatches { get; set; }

        [XmlElement("PeriodForNextMatches")]
        public TimeSpan PeriodForNextMatches { get; set; }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlType("parameters")]
    public class ProjectParametersPackage
    {
        public bool UseTeamVenues { get; set; }

        public TimeSpan MatchStartTime { get; set; }

        public TimeSpan RotationTime { get; set; }

        public TimeSpan MinimumRestTime { get; set; }
    }
}

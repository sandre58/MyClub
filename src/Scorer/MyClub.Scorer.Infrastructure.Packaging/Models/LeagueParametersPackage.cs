// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlType("leagueParameters")]
    public class LeagueParametersPackage : ParametersPackage
    {
        public SchedulingParametersPackage? SchedulingParameters { get; set; }
    }
}

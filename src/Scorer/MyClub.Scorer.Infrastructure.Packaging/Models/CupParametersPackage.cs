// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlType("cupParameters")]
    public class CupParametersPackage : ParametersPackage
    {
        public SchedulingParametersPackage? DefaultSchedulingParameters { get; set; }

        [XmlArray("schedulingParametersList")]
        [XmlArrayItem("schedulingParameters", typeof(SchedulingParametersByItemPackage))]
        public List<SchedulingParametersByItemPackage>? SchedulingParameters { get; set; }
    }
}

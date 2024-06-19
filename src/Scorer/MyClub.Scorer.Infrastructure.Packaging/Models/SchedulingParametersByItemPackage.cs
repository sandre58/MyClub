// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlType("schedulingParameters")]
    public class SchedulingParametersByItemPackage : SchedulingParametersPackage
    {
        [XmlAttribute("itemId")]
        public Guid? ItemId { get; set; }
    }
}

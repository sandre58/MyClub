// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("tactic")]
    public class TacticPackage : LabelPackage
    {
        [XmlArray("positions")]
        [XmlArrayItem("position", typeof(TacticPositionPackage))]
        public List<TacticPositionPackage>? Positions { get; set; }

        [XmlAttribute("instructions")]
        public string Instructions { get; set; } = string.Empty;
    }
}

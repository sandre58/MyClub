// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("squads", Namespace = XmlConstants.MyClubNamespace)]
    public class SquadsPackage : List<SquadPackage>
    {
        public SquadsPackage() { }

        public SquadsPackage(IEnumerable<SquadPackage> enumerable) : base(enumerable)
        {
        }
    }
}

// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlRoot("teams", Namespace = XmlConstants.MyClubNamespace)]
    public class TeamsPackage : List<TeamPackage>
    {
        public TeamsPackage() { }

        public TeamsPackage(IEnumerable<TeamPackage> enumerable) : base(enumerable) { }
    }
}

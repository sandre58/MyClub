// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("clubs", Namespace = XmlConstants.MyClubNamespace)]
    public class ClubsPackage : List<ClubPackage>
    {
        public ClubsPackage() { }

        public ClubsPackage(IEnumerable<ClubPackage> enumerable) : base(enumerable) { }
    }
}

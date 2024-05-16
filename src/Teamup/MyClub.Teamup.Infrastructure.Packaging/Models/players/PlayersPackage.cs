// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("players", Namespace = XmlConstants.MyClubNamespace)]
    public class PlayersPackage : List<PlayerPackage>
    {
        public PlayersPackage() { }

        public PlayersPackage(IEnumerable<PlayerPackage> enumerable) : base(enumerable) { }
    }
}

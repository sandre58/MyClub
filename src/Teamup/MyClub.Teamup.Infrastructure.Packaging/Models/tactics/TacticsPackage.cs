// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("tactics", Namespace = XmlConstants.MyClubNamespace)]
    public class TacticsPackage : List<TacticPackage>
    {
        public TacticsPackage() { }

        public TacticsPackage(IEnumerable<TacticPackage> enumerable) : base(enumerable) { }
    }
}

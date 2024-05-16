// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("friendlies", Namespace = XmlConstants.MyClubNamespace)]
    public class FriendliesPackage : List<FriendlyPackage>
    {
        public FriendliesPackage() { }

        public FriendliesPackage(IEnumerable<FriendlyPackage> enumerable) : base(enumerable) { }
    }
}

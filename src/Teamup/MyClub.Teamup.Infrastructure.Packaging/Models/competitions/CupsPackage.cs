// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("cups", Namespace = XmlConstants.MyClubNamespace)]
    public class CupsPackage : List<CupPackage>
    {
        public CupsPackage() { }

        public CupsPackage(IEnumerable<CupPackage> enumerable) : base(enumerable) { }
    }
}

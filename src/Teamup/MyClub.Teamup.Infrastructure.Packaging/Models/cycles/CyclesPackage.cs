// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("cycles", Namespace = XmlConstants.MyClubNamespace)]
    public class CyclesPackage : List<CyclePackage>
    {
        public CyclesPackage() { }

        public CyclesPackage(IEnumerable<CyclePackage> enumerable) : base(enumerable)
        {
        }
    }
}

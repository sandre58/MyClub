// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("holidays", Namespace = XmlConstants.MyClubNamespace)]
    public class HolidaysPackage : List<HolidaysItemPackage>
    {
        public HolidaysPackage() { }

        public HolidaysPackage(IEnumerable<HolidaysItemPackage> enumerable) : base(enumerable) { }
    }
}

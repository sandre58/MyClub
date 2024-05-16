// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("seasons", Namespace = XmlConstants.MyClubNamespace)]
    public class SeasonsPackage : List<SeasonPackage>
    {
        public SeasonsPackage() { }

        public SeasonsPackage(IEnumerable<SeasonPackage> enumerable) : base(enumerable) { }
    }
}

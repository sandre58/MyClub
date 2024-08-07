﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlRoot("Stadiums", Namespace = XmlConstants.MyClubNamespace)]
    public class StadiumsPackage : List<StadiumPackage>
    {
        public StadiumsPackage() { }

        public StadiumsPackage(IEnumerable<StadiumPackage> enumerable) : base(enumerable) { }
    }
}

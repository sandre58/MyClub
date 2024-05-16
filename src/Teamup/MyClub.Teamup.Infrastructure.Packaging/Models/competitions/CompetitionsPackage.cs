// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("competitions", Namespace = XmlConstants.MyClubNamespace)]
    public class CompetitionsPackage
    {
        public CupsPackage? Cups { get; set; }

        public LeaguesPackage? Leagues { get; set; }

        public FriendliesPackage? Friendlies { get; set; }

    }
}

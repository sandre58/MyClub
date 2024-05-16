// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    [XmlInclude(typeof(LeaguePackage))]
    [XmlInclude(typeof(CupPackage))]
    [XmlInclude(typeof(TournamentPackage))]
    public class CompetitionPackage : AuditablePackage
    {
    }
}

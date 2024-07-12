// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class ProjectPackage
    {
        public MetadataPackage? Metadata { get; set; }

        public CompetitionPackage? Competition { get; set; }

        public TeamsPackage? Teams { get; set; }

        public StadiumsPackage? Stadiums { get; set; }
    }
}

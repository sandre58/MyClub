// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class ProjectPackage
    {
        public MetadataPackage? Metadata { get; set; }

        public HolidaysPackage? Holidays { get; set; }

        public TacticsPackage? Tactics { get; set; }

        public SendedMailsPackage? SendedMails { get; set; }

        public TrainingSessionsPackage? TrainingSessions { get; set; }

        public CyclesPackage? Cycles { get; set; }

        public CompetitionsPackage? Competitions { get; set; }

        public ClubsPackage? Clubs { get; set; }

        public StadiumsPackage? Stadiums { get; set; }

        public PlayersPackage? Players { get; set; }

        public SquadsPackage? Squads { get; set; }

        public SeasonsPackage? Seasons { get; set; }
    }
}

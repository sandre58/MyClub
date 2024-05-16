// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.CrossCutting.Packaging
{
    public static class XmlConstants
    {
        public const string MyClubNamespace = "http://myclub.net/package";

        public const string ContentTypeXml = "application/xml";
        public const string ContentTypePng = "image/png";

        public const string PlayerPhotoUri = "/images/players/{0}.png";
        public const string ClubLogoUri = "/images/clubs/{0}.png";
        public const string CompetitionLogoUri = "/images/competitions/{0}.png";
        public const string PlayersUri = "/players.xml";
        public const string TrainingSessionsUri = "/trainingSessions.xml";
        public const string SquadsUri = "/squads.xml";
        public const string TacticsUri = "/tactics.xml";
        public const string ClubsUri = "/clubs.xml";
        public const string CompetitionsUri = "/competitions.xml";
        public const string StadiumsUri = "/stadiums.xml";
        public const string SendedMailsUri = "/sendedMails.xml";
        public const string HolidaysUri = "/holidays.xml";
        public const string CyclesUri = "/cycles.xml";
        public const string SeasonsUri = "/seasons.xml";
    }
}

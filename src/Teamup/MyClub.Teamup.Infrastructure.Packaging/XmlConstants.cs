// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Teamup.Infrastructure.Packaging
{
    internal static class XmlConstants
    {
        internal const string MyClubNamespace = "http://myclub.net/package";

        internal const string ContentTypeXml = "application/xml";
        internal const string ContentTypePng = "image/png";

        internal const string PlayerPhotoUri = "/images/players/{0}.png";
        internal const string ClubLogoUri = "/images/clubs/{0}.png";
        internal const string CompetitionLogoUri = "/images/competitions/{0}.png";
        internal const string PlayersUri = "/players.xml";
        internal const string TrainingSessionsUri = "/trainingSessions.xml";
        internal const string SquadsUri = "/squads.xml";
        internal const string TacticsUri = "/tactics.xml";
        internal const string ClubsUri = "/clubs.xml";
        internal const string CompetitionsUri = "/competitions.xml";
        internal const string StadiumsUri = "/stadiums.xml";
        internal const string SendedMailsUri = "/sendedMails.xml";
        internal const string HolidaysUri = "/holidays.xml";
        internal const string CyclesUri = "/cycles.xml";
        internal const string SeasonsUri = "/seasons.xml";
        internal const string MetadataUri = "/metadata.xml";
        internal const string ProjectImageUri = "/images/thumbnail.png";
    }
}

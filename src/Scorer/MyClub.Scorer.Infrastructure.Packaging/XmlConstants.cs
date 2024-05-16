// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Infrastructure.Packaging
{
    internal static class XmlConstants
    {
        internal const string MyClubNamespace = "http://myclub.net/package";

        internal const string ContentTypeXml = "application/xml";
        internal const string ContentTypePng = "image/png";

        internal const string PlayerPhotoUri = "/images/players/{0}.png";
        internal const string StaffPhotoUri = "/images/staff/{0}.png";
        internal const string MetadataUri = "/metadata.xml";
        internal const string ProjectImageUri = "/images/thumbnail.png";
        internal const string TeamLogoUri = "/images/teams/{0}.png";
        internal const string TeamsUri = "/teams.xml";
        internal const string StadiumsUri = "/stadiums.xml";
        internal const string CompetitionUri = "/competition.xml";
    }
}

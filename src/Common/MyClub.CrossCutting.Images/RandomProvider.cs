// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using MyClub.CrossCutting.Images.Resources;
using MyNet.Utilities;
using MyNet.Utilities.Generator;

namespace MyClub.CrossCutting.Images
{
    public static class RandomProvider
    {
        private const int CountTeamLogos = 1117;
        private const int CountCompetitionLogos = 90;
        private const int CountMalesFaces = 150;
        private const int CountFemalesFaces = 45;

        public static byte[]? GetCompetitionLogo()
        {
            var numCompetitionLogo = RandomGenerator.Int(1, CountCompetitionLogos);
            return (byte[]?)LogosResources.ResourceManager.GetObject($"competition_{numCompetitionLogo:000}");
        }

        public static byte[]? GetTeamLogo()
        {
            var numTeamLogo = RandomGenerator.Int(1, CountTeamLogos);
            return (byte[]?)LogosResources.ResourceManager.GetObject($"club_{numTeamLogo:0000}");
        }

        public static byte[]? GetPhoto(GenderType? genderType = null)
        {
            var gender = genderType ?? RandomGenerator.Enum<GenderType>();
            var numFace = RandomGenerator.Int(1, gender == GenderType.Male ? CountMalesFaces : CountFemalesFaces);
            return (byte[]?)FacesResources.ResourceManager.GetObject($"{gender.ToString().ToLowerInvariant()}_{numFace:000}", CultureInfo.InvariantCulture);
        }
    }
}

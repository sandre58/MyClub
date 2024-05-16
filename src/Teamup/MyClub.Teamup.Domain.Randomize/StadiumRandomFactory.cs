// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Generator.Extensions;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Domain.Randomize
{
    public static class StadiumRandomFactory
    {
        public static Stadium Create()
        {
            var ground = RandomGenerator.Enum<Ground>();

            var item = new Stadium(MyClubResources.Stadium + $" {NameGenerator.LastName()}", ground)
            {
                Address = new Address(
                    AddressGenerator.Street(),
                    AddressGenerator.PostalCode(),
                    AddressGenerator.City().ToSentence(),
                    RandomGenerator.Country(),
                    AddressGenerator.Coordinates().Latitude,
                    AddressGenerator.Coordinates().Longitude),
            };

            return item;
        }
    }
}

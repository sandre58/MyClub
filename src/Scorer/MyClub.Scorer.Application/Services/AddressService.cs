// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.CrossCutting.Localization;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Google.Maps;

namespace MyClub.Scorer.Application.Services
{
    public class AddressService(ILocationService locationService)
    {
        private readonly ILocationService _locationService = locationService;

        public Coordinates? GetCoordinatesFromAddress(Address address)
        {
            try
            {
                return _locationService.GetCoordinatesFromAddress(address);
            }
            catch (QueryLimitExceededException e)
            {
                throw new TranslatableException(e.Message, nameof(MyClubResources.GoogleQueryLimitExceededError));
            }
            catch (RequestDeniedException e)
            {
                throw new TranslatableException(e.Message, nameof(MyClubResources.GoogleRequestDenied));
            }
        }
    }
}

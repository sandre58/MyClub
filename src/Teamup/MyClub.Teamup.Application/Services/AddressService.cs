// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Google.Maps;

namespace MyClub.Teamup.Application.Services
{
    public class AddressService(ISquadPlayerRepository playerRepository, IStadiumRepository stadiumRepository, IProjectRepository projectRepository, ILocationService locationService)
    {
        private readonly ISquadPlayerRepository _playerRepository = playerRepository;
        private readonly IStadiumRepository _stadiumRepository = stadiumRepository;
        private readonly ILocationService _locationService = locationService;
        private readonly IProjectRepository _projectRepository = projectRepository;

        public IEnumerable<(string PostalCode, string City)> GetPostalCodesAndCities() => _projectRepository.HasCurrent() ? _playerRepository.GetAll().Select(x => x.Player.Address)
                                                                                    .Concat(_stadiumRepository.GetAll().Select(x => x.Address))
                                                                                    .NotNull()
                                                                                    .Where(x => x.PostalCode is not null && x.City is not null)
                                                                                    .Select(x => (x.PostalCode!, x.City!))
                                                                                    .Distinct() : [];

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

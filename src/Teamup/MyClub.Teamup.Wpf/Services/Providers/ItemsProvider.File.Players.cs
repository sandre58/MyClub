// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Wpf.Collections;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Import;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class PlayersFileProvider : ItemsFileProvider<PlayerImportableViewModel>
    {
        private readonly PlayersImportService _playersImportService;
        private readonly PlayerService _playerService;

        public PlayersFileProvider(PlayersImportService playersImportService, PlayerService playerService, Func<PlayerImportableViewModel, bool>? predicate = null) : base(predicate: predicate)
        {
            _playerService = playerService;
            _playersImportService = playersImportService;
        }

        protected override (IEnumerable<PlayerImportableViewModel> items, IEnumerable<Exception> exceptions) LoadItems(string filename)
        {
            ICollection<PlayerImportableViewModel> importablePlayers;

            var (players, exceptions) = _playersImportService.ExtractPlayers(filename);

            importablePlayers = players.Select(x =>
            {
                var isSimilar = _playerService.GetSimilarPlayers(x.FirstName.OrEmpty(), x.LastName.OrEmpty()).Any();
                var player = new PlayerImportableViewModel(x.LastName.OrEmpty(), x.FirstName.OrEmpty(), _playerService, isSimilar ? ImportMode.Update : ImportMode.Add)
                {
                    Team = x.TeamId is not null ? TeamsCollection.All.GetByIdOrDefault(x.TeamId.Value) : null,
                    Photo = x.Photo,
                    Gender = x.Gender,
                    Category = x.Category,
                    FromDate = x.FromDate,
                    Birthdate = x.Birthdate,
                    PlaceOfBirth = x.PlaceOfBirth,
                    Country = x.Country,
                    LicenseNumber = x.LicenseNumber,
                    LicenseState = x.LicenseState,
                    IsMutation = x.IsMutation,
                    Description = x.Description,
                    Laterality = x.Laterality,
                    Size = x.Size,
                    City = x.Address?.City,
                    PostalCode = x.Address?.PostalCode,
                    Address = x.Address?.Street
                };
                player.Height.Value = x.Height;
                player.Weight.Value = x.Weight;
                player.Number.Value = x.Number;
                player.ShoesSize.Value = x.ShoesSize;

                if (x.Emails is not null)
                    player.Emails.AddRange(x.Emails.Select(y => new Email(y.Value.OrEmpty(), y.Label, y.Default)));

                if (x.Phones is not null)
                    player.Phones.AddRange(x.Phones.Select(y => new Phone(y.Value.OrEmpty(), y.Label, y.Default)));

                if (x.Positions is not null)
                    player.Positions.AddRange(x.Positions.Select(y => new RatedPosition(y.Position!, y.Rating) { IsNatural = y.IsNatural }));

                if (x.Injuries is not null)
                    player.Injuries.AddRange(x.Injuries.Select(y => new Injury(y.Date, y.Condition.OrEmpty(), y.Severity, y.EndDate, y.Type, y.Category) { Description = y.Description }));

                player.ValidateProperties();
                return player;
            }).ToList();

            return new(importablePlayers, exceptions);
        }
    }
}

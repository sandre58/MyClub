// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.Randomize;
using MyClub.UnitTests.Teamup.Application.Fake;
using MyNet.Utilities;
using Xunit;

namespace MyClub.UnitTests.Teamup.Application.Services
{

    public class PlayerServiceTests
    {
        [Fact]
        public void ListContainsPlayerWhenSaveNewPlayer()
        {
            var repository = new PlayerRepositoryMock();
            var service = new PlayerService(repository, new ProjectRepositoryMock(), new InjuriesStatisticsRefreshDeferrer(), new TrainingStatisticsRefreshDeferrer());

            var dto = PlayerRandomFactory.Random().To(x => new SquadPlayerDto
            {
                Id = x.Id,
                LastName = x.LastName,
                FirstName = x.FirstName,
            })!;
            service.Save(dto);

            Assert.Single(repository.Players);
            Assert.NotEqual(repository.Players[0].Id, dto.Id);
        }

        [Fact]
        public void PlayersAddedWhenImportNewPlayers()
        {
            var repository = new PlayerRepositoryMock();
            var service = new PlayerService(repository, new ProjectRepositoryMock(), new InjuriesStatisticsRefreshDeferrer(), new TrainingStatisticsRefreshDeferrer());

            var importPlayers = PlayerRandomFactory.RandomSquadPlayers().Select(x => x.To(x => new SquadPlayerDto
            {
                Id = x.Id,
                LastName = x.Player.LastName,
                FirstName = x.Player.FirstName,
            })).ToList();

            service.Import(importPlayers!);

            Assert.Equal(importPlayers.Count, repository.Players.Count);
        }
    }
}

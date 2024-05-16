// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Teamup.Domain.Randomize;
using MyClub.Teamup.Infrastructure.Repositories;
using MyClub.UnitTests.Teamup.Infrastructure.Repositories.Fakes;
using Xunit;

namespace MyClub.UnitTests.Teamup.Infrastructure.Repositories
{
    public class PlayerRepositoryTests
    {
        [Fact]
        public void GetAllReturnAllPlayers()
        {
            var context = new ProjectRepositoryStub();
            var auditService = new AuditServiceStub();
            var repository = new SquadPlayerRepository(context, auditService);

            var result = repository.GetAll();

            Assert.Equal(context.GetCurrentOrThrow()?.Players.Count, result.Count());
        }

        [Fact]
        public void ReturnSpecifiedPlayerWhenIdIsFound()
        {
            var context = new ProjectRepositoryStub();
            var auditService = new AuditServiceStub();
            var repository = new SquadPlayerRepository(context, auditService);

            var result = repository.GetById(context.GetCurrentOrThrow().Players[0].Id);

            Assert.Equal(context.GetCurrentOrThrow().Players[0], result);
        }

        [Fact]
        public void ReturnNullWhenNotFoudId()
        {
            var context = new ProjectRepositoryStub();
            var auditService = new AuditServiceStub();
            var repository = new SquadPlayerRepository(context, auditService);

            var result = repository.GetById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public void PlayerDeletedWhenProvideCorrectId()
        {
            var context = new ProjectRepositoryStub();
            var auditService = new AuditServiceStub();
            var repository = new SquadPlayerRepository(context, auditService);

            var count = context.GetCurrentOrThrow().Players.Count;
            var result = repository.Remove(context.GetCurrentOrThrow().Players[0].Id);
            var newCount = context.GetCurrentOrThrow().Players.Count;

            Assert.True(result);
            Assert.Equal(count - 1, newCount);
        }

        [Fact]
        public void ThrowWhenProvideNotFoundId()
        {
            var context = new ProjectRepositoryStub();
            var auditService = new AuditServiceStub();
            var repository = new SquadPlayerRepository(context, auditService);

            Assert.Throws<ArgumentException>(() => repository.Remove(Guid.NewGuid()));
        }

        [Fact]
        public void PlayerAddWhenProvideCorrectPlayer()
        {
            var context = new ProjectRepositoryStub();
            var auditService = new AuditServiceStub();
            var repository = new SquadPlayerRepository(context, auditService);

            var newPlayer = PlayerRandomFactory.Random();
            var newSquadPlayer = PlayerRandomFactory.RandomSquadPlayer(newPlayer);
            var count = context.GetCurrentOrThrow()?.Players.Count;
            var result = repository.Insert(newSquadPlayer);
            var newCount = context.GetCurrentOrThrow().Players.Count;

            Assert.Equal(count + 1, newCount);
            Assert.Equal(context.GetCurrentOrThrow().Players[context.GetCurrentOrThrow().Players.Count - 1], result);
            Assert.Equal(AuditServiceStub.CreatedBy, result.CreatedBy);
            Assert.Equal(AuditServiceStub.CreatedAt, result.CreatedAt);
            Assert.Null(result.ModifiedBy);
            Assert.Null(result.ModifiedAt);
        }

        [Fact]
        public void PlayerUpdatedWhenProvideFoundPlayer()
        {
            var context = new ProjectRepositoryStub();
            var auditService = new AuditServiceStub();
            var repository = new SquadPlayerRepository(context, auditService);

            var count = context.GetCurrentOrThrow().Players.Count;
            var playerToUpdate = context.GetCurrentOrThrow().Players[0];
            playerToUpdate.Number = 10;
            var result = repository.Update(playerToUpdate);
            var newCount = context.GetCurrentOrThrow().Players.Count;

            Assert.Equal(count, newCount);
            Assert.Equal(context.GetCurrentOrThrow().Players[0], result);
            Assert.Equal(10, context.GetCurrentOrThrow().Players[0].Number);
            Assert.Equal(AuditServiceStub.ModifiedBy, result.ModifiedBy);
            Assert.Equal(AuditServiceStub.ModifiedAt, result.ModifiedAt);
        }
    }
}

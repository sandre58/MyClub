// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using Xunit;

namespace MyClub.UnitTests.Domain
{
    public class EntityTests
    {
        private class EntityFake(Guid? id = null) : Entity(id)
        {
        }

        [Fact]
        public void IdIsNotNullWhenCreateEntityWithId()
        {
            var guid = new Guid("79ae7001-a138-41f9-ab88-24e6143ca517");
            var entity = new EntityFake(guid);

            Assert.Equal(guid, entity.Id);
        }

        [Fact]
        public void IdIsNotNullWhenCreateEntityWithoutId()
        {
            var entity = new EntityFake();

            Assert.True(entity.Id != Guid.Empty);
        }

        [Fact]
        public void EntitiesAreEqualsWhenEntitiesAreSameId()
        {
            var guid = new Guid("79ae7001-a138-41f9-ab88-24e6143ca517");
            var entity1 = new EntityFake(guid);
            var entity2 = new EntityFake(guid);

            Assert.True(Equals(entity1, entity2));
            Assert.True(entity1.Equals(entity2));
        }

        [Fact]
        public void EntitiesAreNotEqualsWhenEntitiesAreNotSameId()
        {
            var guid1 = new Guid("79ae7001-a138-41f9-ab88-24e6143ca517");
            var guid2 = new Guid("ac83013f-3707-4086-b4b3-5e0f35664f98");
            var entity1 = new EntityFake(guid1);
            var entity2 = new EntityFake(guid2);

            Assert.False(Equals(entity1, entity2));
            Assert.False(entity1.Equals(entity2));
        }
    }
}

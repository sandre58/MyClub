// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using Xunit;

namespace MyClub.UnitTests.Domain
{
    public class AuditableEntityTests
    {
        private class AuditableEntityFake(Guid? id = null) : AuditableEntity(id)
        {
        }

        [Fact]
        public void CreationPropertiesAreUpdatedWhenMarkedAsCreated()
        {
            var date = DateTime.UtcNow;
            var user = "System";
            var entity = new AuditableEntityFake();
            entity.MarkedAsCreated(date, user);

            Assert.Equal(date, entity.CreatedAt);
            Assert.Equal(user, entity.CreatedBy);
            Assert.Null(entity.ModifiedAt);
            Assert.Null(entity.ModifiedBy);
        }

        [Fact]
        public void ModificationPropertiesAreUpdatedWhenMarkedAsModified()
        {
            var date = DateTime.UtcNow;
            var user = "System";
            var entity = new AuditableEntityFake();
            entity.MarkedAsModified(date, user);

            Assert.Equal(date, entity.ModifiedAt);
            Assert.Equal(user, entity.ModifiedBy);
        }
    }
}

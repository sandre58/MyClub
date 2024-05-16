// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyClub.Domain.Services;

namespace MyClub.UnitTests.Teamup.Infrastructure.Repositories.Fakes
{
    public class AuditServiceStub : IAuditService
    {
        public const string CreatedBy = "CreatedBy";
        public const string ModifiedBy = "ModifiedBy";
        public static readonly DateTime CreatedAt = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static readonly DateTime ModifiedAt = new(2022, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc);

        public void New(IAuditable auditable) => auditable.MarkedAsCreated(CreatedAt, CreatedBy);

        public void Update(IAuditable auditable) => auditable.MarkedAsModified(ModifiedAt, ModifiedBy);
    }
}

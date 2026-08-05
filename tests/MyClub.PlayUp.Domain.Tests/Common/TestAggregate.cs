// -----------------------------------------------------------------------
// <copyright file="TestAggregate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Tests.Common;

internal sealed class TestAggregate(CompetitionId id) : AggregateRoot<CompetitionId>(id)
{
    public void DoSomething(IDomainEvent domainEvent) => Raise(domainEvent);
}

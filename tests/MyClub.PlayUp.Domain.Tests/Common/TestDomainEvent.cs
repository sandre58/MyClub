// -----------------------------------------------------------------------
// <copyright file="TestDomainEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Tests.Common;

internal sealed record TestDomainEvent : DomainEvent
{
    public TestDomainEvent(DateTimeOffset occurredOn, string name)
        : base(occurredOn) => Name = name;

    public TestDomainEvent(IClock clock, string name)
        : base(clock) => Name = name;

    public string Name { get; }
}

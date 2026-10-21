// -----------------------------------------------------------------------
// <copyright file="DomainExceptionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Common;

public sealed class DomainExceptionTests
{
    [Fact]
    public void Domain_exception_carries_the_business_message()
    {
        // Arrange / Act
        var exception = new DomainException("Structure is locked while Running.");

        // Assert
        exception.Message.Should().Be("Structure is locked while Running.");
        exception.Code.Should().BeNull();
    }

    [Fact]
    public void Domain_exception_can_carry_a_stable_business_code()
    {
        // Arrange / Act
        var exception = new DomainException("Entry already exists for this team.", "ENTRY_DUPLICATE_TEAM");

        // Assert
        exception.Message.Should().Be("Entry already exists for this team.");
        exception.Code.Should().Be("ENTRY_DUPLICATE_TEAM");
    }
}

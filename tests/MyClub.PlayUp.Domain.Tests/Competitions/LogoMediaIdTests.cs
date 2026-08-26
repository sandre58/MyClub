// -----------------------------------------------------------------------
// <copyright file="LogoMediaIdTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Competitions;

public sealed class LogoMediaIdTests
{
    [Fact]
    public void Create_WithNull_ReturnsNull()
    {
        LogoMediaId.Create(null).Should().BeNull();
    }

    [Fact]
    public void Create_WithGuid_ReturnsTypedId()
    {
        var guid = Guid.CreateVersion7();
        LogoMediaId.Create(guid)!.Value.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_WithEmpty_Throws()
    {
        var act = () => new LogoMediaId(Guid.Empty);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidLogoMediaId);
    }
}

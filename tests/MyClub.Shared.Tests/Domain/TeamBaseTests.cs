// -----------------------------------------------------------------------
// <copyright file="TeamBaseTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using FluentAssertions;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Events;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Geography;
using Xunit;

namespace MyClub.Shared.Tests.Domain;

public class TeamBaseTests
{
    #region Test Helper Classes

    // Test TeamId implementation
    internal sealed record TestTeamId(Guid Value) : EntityId<TestTeamId>(Value)
    {
        public static new TestTeamId New() => EntityId.New<TestTeamId>();
    }

    // Test TeamBase implementation
    private sealed class TestTeam(TestTeamId id, string name, string? shortName = null) : TeamBase<TestTeamId>(id, name, shortName)
    {
        public void AddTestDomainEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    // Test Domain Event implementation
    private sealed record TestDomainEvent(string Message) : IDomainEvent
    {
        public DateTimeOffset OccurredOn { get; } = DateTime.UtcNow;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithIdAndName_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = TestTeamId.New();
        const string name = "Test Team";

        // Act
        var team = new TestTeam(id, name);

        // Assert
        team.Id.Should().Be(id);
        team.DisplayName.Name.Should().Be(name);
        team.DisplayName.ShortName.Should().Be("TT"); // DisplayName auto-generates shortName from name initials
        team.Logo.Should().BeNull();
        team.Country.Should().BeNull();
        team.HomeColor.Should().BeNull();
        team.AwayColor.Should().BeNull();
        team.StadiumId.Should().BeNull();
        team.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithIdNameAndShortName_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = TestTeamId.New();
        const string name = "Test Team";
        const string shortName = "TT";

        // Act
        var team = new TestTeam(id, name, shortName);

        // Assert
        team.Id.Should().Be(id);
        team.DisplayName.Name.Should().Be(name);
        team.DisplayName.ShortName.Should().Be(shortName);
        team.Logo.Should().BeNull();
        team.Country.Should().BeNull();
        team.HomeColor.Should().BeNull();
        team.AwayColor.Should().BeNull();
        team.StadiumId.Should().BeNull();
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void DisplayName_ShouldBeSettable()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Original Team");
        var newDisplayName = new DisplayName("New Team", "NT");

        // Act
        team.DisplayName = newDisplayName;

        // Assert
        team.DisplayName.Should().Be(newDisplayName);
        team.DisplayName.Name.Should().Be("New Team");
        team.DisplayName.ShortName.Should().Be("NT");
    }

    [Fact]
    public void Logo_ShouldBeSettable()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");
        var logo = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        team.Logo = logo;

        // Assert
        team.Logo.Should().BeEquivalentTo(logo);
    }

    [Fact]
    public void Country_ShouldBeSettable()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");
        var country = Country.France;

        // Act
        team.Country = country;

        // Assert
        team.Country.Should().Be(country);
    }

    [Fact]
    public void StadiumId_ShouldBeSettable()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");
        var stadiumId = new StadiumId(Guid.NewGuid());

        // Act
        team.StadiumId = stadiumId;

        // Assert
        team.StadiumId.Should().Be(stadiumId);
    }

    [Fact]
    public void HomeColor_ShouldBeSettable()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");
        const string homeColor = "#FF0000";

        // Act
        team.HomeColor = homeColor;

        // Assert
        team.HomeColor.Should().Be(homeColor);
    }

    [Fact]
    public void AwayColor_ShouldBeSettable()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");
        const string awayColor = "#0000FF";

        // Act
        team.AwayColor = awayColor;

        // Assert
        team.AwayColor.Should().Be(awayColor);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void TeamBase_ShouldInheritFromAuditableEntity()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");

        // Assert
        team.Should().BeAssignableTo<AuditableEntity<TestTeamId>>();
        team.Should().BeAssignableTo<Entity<TestTeamId>>();
        team.Should().BeAssignableTo<IEntity<TestTeamId>>();
        team.Should().BeAssignableTo<IAuditableEntity<TestTeamId>>();
    }

    [Fact]
    public void TeamBase_ShouldImplementITeam()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");

        // Assert
        team.Should().BeAssignableTo<ITeam>();
    }

    [Fact]
    public void TeamBase_ShouldSupportDomainEvents()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");
        var domainEvent = new TestDomainEvent("Test event");

        // Act
        team.AddTestDomainEvent(domainEvent);

        // Assert
        team.DomainEvents.Should().HaveCount(1);
        team.DomainEvents.Should().Contain(domainEvent);
    }

    [Fact]
    public void TeamBase_ShouldSupportAuditProperties()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");
        var now = DateTime.UtcNow;

        // Act
        team.MarkedAsCreated(now, "TestUser");

        // Assert
        team.CreatedAt.Should().Be(now);
        team.CreatedBy.Should().Be("TestUser");
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        // Arrange
        const string teamName = "Manchester United";
        var team = new TestTeam(TestTeamId.New(), teamName, "MU");

        // Act
        var result = team.ToString();

        // Assert
        result.Should().Be(teamName);
    }

    [Fact]
    public void ToString_WithShortName_ShouldReturnDisplayName()
    {
        // Arrange
        const string teamName = "Manchester United";
        const string shortName = "MU";
        var team = new TestTeam(TestTeamId.New(), teamName, shortName);

        // Act
        var result = team.ToString();

        // Assert
        result.Should().Be(teamName); // ToString returns the full name, not short name
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void CompareTo_ITeam_ShouldCompareByDisplayName()
    {
        // Arrange
        var team1 = new TestTeam(TestTeamId.New(), "Arsenal");
        var team2 = new TestTeam(TestTeamId.New(), "Barcelona");
        var team3 = new TestTeam(TestTeamId.New(), "Arsenal");

        // Act & Assert
        team1.CompareTo(team2).Should().BeLessThan(0); // Arsenal < Barcelona
        team2.CompareTo(team1).Should().BeGreaterThan(0); // Barcelona > Arsenal
        team1.CompareTo(team3).Should().Be(0); // Arsenal == Arsenal
    }

    [Fact]
    public void CompareTo_ITeam_WithNull_ShouldReturn1()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");

        // Act
        var result = team.CompareTo(null);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_Entity_WithSameTeam_ShouldReturnZero()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");

        // Act
        var result = team.CompareTo(team); // Explicit ITeam cast

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Similarity Tests

    [Fact]
    public void IsSimilar_ITeam_SameDisplayName_ShouldReturnTrue()
    {
        // Arrange
        var team1 = new TestTeam(TestTeamId.New(), "Arsenal", "ARS");
        var team2 = new TestTeam(TestTeamId.New(), "Arsenal", "AFC");

        // Act
        var result = team1.IsSimilar(team2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSimilar_ITeam_DifferentDisplayName_ShouldReturnFalse()
    {
        // Arrange
        var team1 = new TestTeam(TestTeamId.New(), "Arsenal");
        var team2 = new TestTeam(TestTeamId.New(), "Barcelona");

        // Act
        var result = team1.IsSimilar(team2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSimilar_ITeam_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");

        // Act
        var result = team.IsSimilar(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSimilar_Object_WithITeam_ShouldReturnCorrectResult()
    {
        // Arrange
        var team1 = new TestTeam(TestTeamId.New(), "Arsenal");
        var team2 = new TestTeam(TestTeamId.New(), "Arsenal");
        var team3 = new TestTeam(TestTeamId.New(), "Barcelona");

        // Act & Assert
        team1.IsSimilar((object)team2).Should().BeTrue();
        team1.IsSimilar((object)team3).Should().BeFalse();
    }

    [Fact]
    public void IsSimilar_Object_WithNonITeam_ShouldReturnFalse()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");
        const string nonTeam = "Not a team";

        // Act
        var result = team.IsSimilar(nonTeam);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSimilar_Object_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");

        // Act
        var result = team.IsSimilar((object?)null);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_ShouldBeBasedOnId()
    {
        // Arrange
        var id = TestTeamId.New();
        var team1 = new TestTeam(id, "Team1");
        var team2 = new TestTeam(id, "Team2");

        // Set different properties
        team1.HomeColor = "#FF0000";
        team2.AwayColor = "#0000FF";

        // Act & Assert
        team1.Should().Be(team2);
        team1.Equals(team2).Should().BeTrue();
        team1.GetHashCode().Should().Be(team2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentIds_ShouldNotBeEqual()
    {
        // Arrange
        var team1 = new TestTeam(TestTeamId.New(), "Team1");
        var team2 = new TestTeam(TestTeamId.New(), "Team2");

        // Act & Assert
        team1.Should().NotBe(team2);
        team1.Equals(team2).Should().BeFalse();
        team1.GetHashCode().Should().NotBe(team2.GetHashCode());
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var id = TestTeamId.New();
        var emptyName = string.Empty;

        // Act & Assert
        var act = () => new TestTeam(id, emptyName);
        act.Should().Throw<NullOrEmptyException>()
           .Which.Message.Should().Contain("Name"); // DisplayName validates name is not empty
    }

    [Fact]
    public void Constructor_WithEmptyShortName_ShouldSetShortNameToEmpty()
    {
        // Arrange
        var id = TestTeamId.New();
        const string name = "Test Team";
        var emptyShortName = string.Empty;

        // Act
        var team = new TestTeam(id, name, emptyShortName);

        // Assert
        team.DisplayName.Name.Should().Be(name);
        team.DisplayName.ShortName.Should().Be(emptyShortName);
    }

    [Fact]
    public void Logo_WithLargeByteArray_ShouldWork()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team");
        var largeLogo = new byte[1024]; // 1KB logo
        Array.Fill(largeLogo, (byte)0xFF);

        // Act
        team.Logo = largeLogo;

        // Assert
        team.Logo.Should().BeEquivalentTo(largeLogo);
        team.Logo.Should().HaveCount(1024);
    }

    [Fact]
    public void Properties_SetToNull_ShouldAcceptNullValues()
    {
        // Arrange
        var team = new TestTeam(TestTeamId.New(), "Test Team")
        {
            Logo = [1, 2, 3],
            Country = Country.France,
            StadiumId = new(Guid.NewGuid()),
            HomeColor = "#FF0000",
            AwayColor = "#0000FF"
        };

        // Act
        team.Logo = null;
        team.Country = null;
        team.StadiumId = null;
        team.HomeColor = null;
        team.AwayColor = null;

        // Assert
        team.Logo.Should().BeNull();
        team.Country.Should().BeNull();
        team.StadiumId.Should().BeNull();
        team.HomeColor.Should().BeNull();
        team.AwayColor.Should().BeNull();
    }

    [Fact]
    public void TeamBase_WithComplexScenario_ShouldWorkCorrectly()
    {
        // Arrange
        var id = TestTeamId.New();
        const string name = "Manchester United";
        const string shortName = "MU";
        var team = new TestTeam(id, name, shortName)
        {
            // Act - Set all properties
            Logo =
                [1, 2, 3, 4],
            Country = Country.France,
            StadiumId = new(Guid.NewGuid()),
            HomeColor = "#FF0000",
            AwayColor = "#FFFFFF"
        };

        team.MarkedAsCreated(DateTime.UtcNow, "System");

        var domainEvent = new TestDomainEvent("Team created");
        team.AddTestDomainEvent(domainEvent);

        // Assert
        team.Id.Should().Be(id);
        team.DisplayName.Name.Should().Be(name);
        team.DisplayName.ShortName.Should().Be(shortName);
        team.Logo.Should().NotBeNull();
        team.Country.Should().Be(Country.France);
        team.StadiumId.Should().NotBeNull();
        team.HomeColor.Should().Be("#FF0000");
        team.AwayColor.Should().Be("#FFFFFF");
        team.CreatedBy.Should().Be("System");
        team.DomainEvents.Should().HaveCount(1);
        team.ToString().Should().Be(name);
        team.DomainEvents.OfType<TestDomainEvent>().First().Message.Should().Be("Team created");
    }

    #endregion

    #region Helper Classes from Other Tests

    // Reuse from AuditableEntityTests to avoid duplication
    internal sealed record TestAuditableEntityId(Guid Value) : EntityId<TestAuditableEntityId>(Value)
    {
        public static new TestAuditableEntityId New() => EntityId.New<TestAuditableEntityId>();
    }

    internal sealed class TestAuditableEntity(TestAuditableEntityId id, string name) : AuditableEntity<TestAuditableEntityId>(id)
    {
        public string Name { get; set; } = name;
    }

    #endregion
}

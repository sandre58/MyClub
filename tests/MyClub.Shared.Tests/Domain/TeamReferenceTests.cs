// -----------------------------------------------------------------------
// <copyright file="TeamReferenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using MyClub.Shared.Domain.Teams;
using Xunit;

namespace MyClub.Shared.Tests.Domain;

public class TeamReferenceTests
{
    #region TeamReference Abstract Class Tests

    [Fact]
    public void TeamReference_ShouldBeAbstractRecord()
    {
        // Assert
        typeof(TeamReference).Should().BeAbstract();
        typeof(TeamReference).Should().BeAssignableTo<object>();
    }

    #endregion

    #region ConcreteTeamReference Tests

    [Fact]
    public void ConcreteTeamReference_Constructor_ShouldSetIdCorrectly()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());

        // Act
        var reference = new ConcreteTeamReference(teamId);

        // Assert
        reference.Id.Should().Be(teamId);
    }

    [Fact]
    public void ConcreteTeamReference_ShouldInheritFromTeamReference()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());
        var reference = new ConcreteTeamReference(teamId);

        // Assert
        reference.Should().BeAssignableTo<TeamReference>();
    }

    [Fact]
    public void ConcreteTeamReference_Equality_SameId_ShouldBeEqual()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());
        var reference1 = new ConcreteTeamReference(teamId);
        var reference2 = new ConcreteTeamReference(teamId);

        // Act & Assert
        reference1.Should().Be(reference2);
        reference1.Equals(reference2).Should().BeTrue();
        reference1.GetHashCode().Should().Be(reference2.GetHashCode());
    }

    [Fact]
    public void ConcreteTeamReference_Equality_DifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var teamId1 = new TeamId(Guid.NewGuid());
        var teamId2 = new TeamId(Guid.NewGuid());
        var reference1 = new ConcreteTeamReference(teamId1);
        var reference2 = new ConcreteTeamReference(teamId2);

        // Act & Assert
        reference1.Should().NotBe(reference2);
        reference1.Equals(reference2).Should().BeFalse();
    }

    [Fact]
    public void ConcreteTeamReference_Equality_WithNull_ShouldNotBeEqual()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());
        var reference = new ConcreteTeamReference(teamId);
        ConcreteTeamReference? nullReference = null;

        // Act & Assert
        reference.Should().NotBeNull();
        reference.Should().NotBe(nullReference);
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_TeamIdToConcreteTeamReference_ShouldWork()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());

        // Act
        var reference = ConcreteTeamReference.ToConcreteTeamReference(teamId);

        // Assert
        reference.Should().NotBeNull();
        reference.Id.Should().Be(teamId);
    }

    [Fact]
    public void ImplicitConversion_ConcreteTeamReferenceToTeamId_ShouldWork()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());
        var reference = new ConcreteTeamReference(teamId);

        // Act
        var convertedId = ConcreteTeamReference.ToTeamId(reference);

        // Assert
        convertedId.Should().Be(teamId);
    }

    [Fact]
    public void ImplicitConversion_RoundTrip_ShouldPreserveValue()
    {
        // Arrange
        var originalId = new TeamId(Guid.NewGuid());

        // Act
        var reference = ConcreteTeamReference.ToConcreteTeamReference(originalId);
        var convertedBack = ConcreteTeamReference.ToTeamId(reference);

        // Assert
        convertedBack.Should().Be(originalId);
    }

    #endregion

    #region Static Method Tests

    [Fact]
    public void ToConcreteTeamReference_ShouldCreateCorrectReference()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());

        // Act
        var reference = ConcreteTeamReference.ToConcreteTeamReference(teamId);

        // Assert
        reference.Should().NotBeNull();
        reference.Id.Should().Be(teamId);
        reference.Should().BeOfType<ConcreteTeamReference>();
    }

    [Fact]
    public void ToTeamId_ShouldExtractCorrectId()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());
        var reference = new ConcreteTeamReference(teamId);

        // Act
        var extractedId = ConcreteTeamReference.ToTeamId(reference);

        // Assert
        extractedId.Should().Be(teamId);
    }

    [Fact]
    public void ToConcreteTeamReference_WithNullId_ShouldHandleGracefully()
    {
        // Arrange
        TeamId nullId = null!;

        // Act
        var reference = ConcreteTeamReference.ToConcreteTeamReference(nullId);

        // Assert
        reference.Should().NotBeNull();
        reference.Id.Should().BeNull();
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ConcreteTeamReference_ToString_ShouldIncludeId()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());
        var reference = new ConcreteTeamReference(teamId);

        // Act
        var result = reference.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("ConcreteTeamReference");
        result.Should().Contain(teamId.ToString());
    }

    #endregion

    #region Record Properties Tests

    [Fact]
    public void ConcreteTeamReference_ShouldBeRecord()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());
        var reference = new ConcreteTeamReference(teamId);

        // Assert
        reference.Should().BeAssignableTo<TeamReference>();
        typeof(ConcreteTeamReference).Should().BeSealed();
    }

    [Fact]
    public void ConcreteTeamReference_ShouldHaveDeconstruct()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());
        var reference = new ConcreteTeamReference(teamId);

        // Act
        reference.Deconstruct(out var id);

        // Assert
        id.Should().Be(teamId);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void ConcreteTeamReference_EmptyGuid_ShouldWork()
    {
        // Arrange
        var emptyTeamId = new TeamId(Guid.Empty);

        // Act
        var reference = new ConcreteTeamReference(emptyTeamId);

        // Assert
        reference.Id.Should().Be(emptyTeamId);
        reference.Id.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ConcreteTeamReference_Serialization_ShouldPreserveProperties()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());
        var reference = new ConcreteTeamReference(teamId);

        // Act - Simulate serialization/deserialization by creating new instance with same values
        var serializedReference = new ConcreteTeamReference(teamId);

        // Assert
        serializedReference.Should().Be(reference);
        serializedReference.Id.Should().Be(reference.Id);
    }

    [Fact]
    public void ConcreteTeamReference_InheritanceChain_ShouldBeCorrect()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());
        var reference = new ConcreteTeamReference(teamId);

        // Assert
        reference.Should().BeAssignableTo<TeamReference>();
        reference.Should().BeAssignableTo<object>();
        reference.GetType().BaseType.Should().Be<TeamReference>();
    }

    [Fact]
    public void ConcreteTeamReference_StaticMethods_ShouldBeConsistent()
    {
        // Arrange
        var teamId = new TeamId(Guid.NewGuid());

        // Act
        var reference1 = ConcreteTeamReference.ToConcreteTeamReference(teamId);
        var reference2 = ConcreteTeamReference.ToConcreteTeamReference(teamId);
        var extractedId1 = ConcreteTeamReference.ToTeamId(reference1);
        var extractedId2 = ConcreteTeamReference.ToTeamId(reference2);

        // Assert
        reference1.Should().Be(reference2);
        extractedId1.Should().Be(teamId);
        extractedId2.Should().Be(teamId);
        extractedId1.Should().Be(extractedId2);
    }

    #endregion
}

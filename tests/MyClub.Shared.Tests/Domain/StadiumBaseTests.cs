// -----------------------------------------------------------------------
// <copyright file="StadiumBaseTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Geography;
using Xunit;

namespace MyClub.Shared.Tests.Domain;

public class StadiumBaseTests
{
    #region Test Concrete Implementation

    private sealed record ConcreteStadiumId(Guid Value) : EntityId<ConcreteStadiumId>(Value);

    private sealed class ConcreteStadium : StadiumBase<ConcreteStadiumId>
    {
        public ConcreteStadium() { } // For EF Core

        public ConcreteStadium(ConcreteStadiumId id, string name, Ground ground)
            : base(id, name, ground) { }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = new ConcreteStadiumId(Guid.NewGuid());
        const string name = "Allianz Arena";
        const Ground ground = Ground.Grass;

        // Act
        var stadium = new ConcreteStadium(id, name, ground);

        // Assert
        stadium.Id.Should().Be(id);
        stadium.DisplayName.Name.Should().Be(name);
        stadium.Ground.Should().Be(ground);
        stadium.Address.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var id = new ConcreteStadiumId(Guid.NewGuid());
        var name = string.Empty;
        const Ground ground = Ground.Grass;

        // Act & Assert
        var act = () => new ConcreteStadium(id, name, ground);

        act.Should().Throw<NullOrEmptyException>();
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowException()
    {
        // Arrange
        var id = new ConcreteStadiumId(Guid.NewGuid());
        string name = null!;
        const Ground ground = Ground.Grass;

        // Act & Assert
        var act = () => new ConcreteStadium(id, name, ground);

        act.Should().Throw<NullOrEmptyException>();
    }

    [Fact]
    public void DefaultConstructor_ShouldCreateStadiumWithNullDisplayName()
    {
        // Act
        var stadium = new ConcreteStadium();

        // Assert
        stadium.DisplayName.Should().BeNull();
        stadium.Ground.Should().Be(Ground.Grass); // Default enum value
        stadium.Address.Should().BeNull();
    }

    #endregion

    #region Property Tests

    [Theory]
    [InlineData(Ground.Grass)]
    [InlineData(Ground.ArtificialGrass)]
    [InlineData(Ground.Sand)]
    [InlineData(Ground.Indoor)]
    public void Ground_SetValue_ShouldReturnCorrectValue(Ground groundType)
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.Grass)
        {
            // Act
            Ground = groundType
        };

        // Assert
        stadium.Ground.Should().Be(groundType);
    }

    [Fact]
    public void DisplayName_SetValue_ShouldReturnCorrectValue()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Original Stadium", Ground.Grass);
        var newDisplayName = new DisplayName("New Stadium Name");

        // Act
        stadium.DisplayName = newDisplayName;

        // Assert
        stadium.DisplayName.Should().Be(newDisplayName);
        stadium.DisplayName.Name.Should().Be("New Stadium Name");
    }

    [Fact]
    public void Address_SetValue_ShouldReturnCorrectValue()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.Grass);
        var address = new Address("123 Main Street", "80331", "Munich", Country.Germany, 48.1351, 11.5820);

        // Act
        stadium.Address = address;

        // Assert
        stadium.Address.Should().Be(address);
        stadium.Address.City.Should().Be("Munich");
    }

    [Fact]
    public void Address_SetToNull_ShouldBeNull()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.Grass);
        var address = new Address(city: "Munich");
        stadium.Address = address;

        // Act
        stadium.Address = null;

        // Assert
        stadium.Address.Should().BeNull();
    }

    #endregion

    #region IStadium Interface Tests

    [Fact]
    public void IStadium_Properties_ShouldBeAccessible()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.ArtificialGrass);
        IStadium stadiumInterface = stadium;

        // Act & Assert
        stadiumInterface.DisplayName.Should().Be(stadium.DisplayName);
        stadiumInterface.Ground.Should().Be(Ground.ArtificialGrass);
        stadiumInterface.Address.Should().BeNull();
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void CompareTo_WithSameDisplayName_ShouldReturnZero()
    {
        // Arrange
        var stadium1 = new ConcreteStadium(new(Guid.NewGuid()), "Stadium A", Ground.Grass);
        var stadium2 = new ConcreteStadium(new(Guid.NewGuid()), "Stadium A", Ground.Sand);

        // Act
        var result = stadium1.CompareTo(stadium2);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CompareTo_WithDifferentDisplayNames_ShouldReturnCorrectOrder()
    {
        // Arrange
        var stadium1 = new ConcreteStadium(new(Guid.NewGuid()), "A Stadium", Ground.Grass);
        var stadium2 = new ConcreteStadium(new(Guid.NewGuid()), "B Stadium", Ground.Grass);

        // Act
        var result = stadium1.CompareTo(stadium2);

        // Assert
        result.Should().BeLessThan(0);
    }

    [Fact]
    public void CompareTo_WithNullOther_ShouldReturnOne()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.Grass);

        // Act
        var result = stadium.CompareTo(null);

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region Similarity Tests

    [Fact]
    public void IsSimilar_WithSameDisplayName_ShouldReturnTrue()
    {
        // Arrange
        var stadium1 = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.Grass);
        var stadium2 = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.ArtificialGrass);

        // Act
        var result = stadium1.IsSimilar(stadium2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSimilar_WithDifferentDisplayNames_ShouldReturnFalse()
    {
        // Arrange
        var stadium1 = new ConcreteStadium(new(Guid.NewGuid()), "Stadium A", Ground.Grass);
        var stadium2 = new ConcreteStadium(new(Guid.NewGuid()), "Stadium B", Ground.Grass);

        // Act
        var result = stadium1.IsSimilar(stadium2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSimilar_WithNullOther_ShouldReturnFalse()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.Grass);

        // Act
        var result = stadium.IsSimilar(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSimilar_WithObjectInterface_ShouldWorkCorrectly()
    {
        // Arrange
        var stadium1 = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.Grass);
        var stadium2 = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.Sand);
        const string nonStadium = "Not a stadium";

        // Act
        var resultSimilar = stadium1.IsSimilar((object)stadium2);
        var resultNotSimilar = stadium1.IsSimilar(nonStadium);

        // Assert
        resultSimilar.Should().BeTrue();
        resultNotSimilar.Should().BeFalse();
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WithNameOnly_ShouldReturnName()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Allianz Arena", Ground.Grass);

        // Act
        var result = stadium.ToString();

        // Assert
        result.Should().Be("Allianz Arena");
    }

    [Fact]
    public void ToString_WithNameAndAddress_ShouldReturnNameAndCity()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Allianz Arena", Ground.Grass) { Address = new(city: "Munich", street: "Werner-Heisenberg-Allee 25") };

        // Act
        var result = stadium.ToString();

        // Assert
        result.Should().Be("Allianz Arena, Munich");
    }

    [Fact]
    public void ToString_WithAddressButNoCity_ShouldReturnNameOnly()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.Grass)
        {
            Address = new(street: "123 Main St") // No city
        };

        // Act
        var result = stadium.ToString();

        // Assert
        result.Should().Be("Test Stadium");
    }

    #endregion

    #region Ground Enum Tests

    [Theory]
    [InlineData(Ground.Grass, "Grass")]
    [InlineData(Ground.ArtificialGrass, "ArtificialGrass")]
    [InlineData(Ground.Sand, "Sand")]
    [InlineData(Ground.Indoor, "Indoor")]
    public void Ground_EnumValues_ShouldHaveCorrectNames(Ground ground, string expectedName) => ground.ToString().Should().Be(expectedName);

    [Fact]
    public void Ground_AllValues_ShouldBeAvailable()
    {
        // Arrange & Act
        var allGrounds = Enum.GetValues<Ground>();

        // Assert
        allGrounds.Should().HaveCount(4);
        allGrounds.Should().Contain(Ground.Grass);
        allGrounds.Should().Contain(Ground.ArtificialGrass);
        allGrounds.Should().Contain(Ground.Sand);
        allGrounds.Should().Contain(Ground.Indoor);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void StadiumBase_ShouldInheritFromAuditableEntity()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.Grass);

        // Act & Assert
        stadium.Should().BeAssignableTo<AuditableEntity<ConcreteStadiumId>>();
    }

    [Fact]
    public void StadiumBase_ShouldImplementIStadium()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Test Stadium", Ground.Grass);

        // Act & Assert
        stadium.Should().BeAssignableTo<IStadium>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void DisplayName_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var id = new ConcreteStadiumId(Guid.NewGuid());
        const string name = "Stadium de l'Épée & des Étoiles";
        const Ground ground = Ground.Grass;

        // Act
        var stadium = new ConcreteStadium(id, name, ground);

        // Assert
        stadium.DisplayName.Name.Should().Be(name);
    }

    [Fact]
    public void DisplayName_WithLongName_ShouldCreateCorrectShortName()
    {
        // Arrange
        var id = new ConcreteStadiumId(Guid.NewGuid());
        const string name = "Very Long Stadium Name With Multiple Words For Testing";
        const Ground ground = Ground.Grass;

        // Act
        var stadium = new ConcreteStadium(id, name, ground);

        // Assert
        stadium.DisplayName.Name.Should().Be(name);
        stadium.DisplayName.ShortName.Should().NotBeNullOrEmpty();
        stadium.DisplayName.ShortName.Length.Should().BeLessThan(name.Length);
    }

    [Fact]
    public void Address_WithCompleteInformation_ShouldStoreAllData()
    {
        // Arrange
        var stadium = new ConcreteStadium(new(Guid.NewGuid()), "Complete Stadium", Ground.Grass);
        var address = new Address("123 Stadium Boulevard", "75001", "Paris", Country.France, 48.8566, 2.3522);

        // Act
        stadium.Address = address;

        // Assert
        stadium.Address.Should().NotBeNull();
        stadium.Address!.Street.Should().Be("123 Stadium Boulevard");
        stadium.Address.City.Should().Be("Paris");
        stadium.Address.PostalCode.Should().Be("75001");
        stadium.Address.Country.Should().Be(Country.France);
        stadium.Address.Latitude.Should().Be(48.8566);
        stadium.Address.Longitude.Should().Be(2.3522);
    }

    #endregion
}

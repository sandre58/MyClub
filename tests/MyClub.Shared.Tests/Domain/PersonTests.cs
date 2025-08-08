// -----------------------------------------------------------------------
// <copyright file="PersonTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using MyClub.Shared.Domain.Extensions;
using MyClub.Shared.Domain.Persons;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Geography;
using Xunit;

namespace MyClub.Shared.Tests.Domain;

public class PersonTests
{
    #region Test Implementation Classes

    // Concrete implementation of EntityId for testing
    internal sealed record ConcretePersonId(Guid Value) : EntityId<ConcretePersonId>(Value)
    {
        public static new ConcretePersonId New() => new(Guid.NewGuid());
    }

    // Concrete implementation of Person for testing
    private sealed class ConcretePerson(ConcretePersonId id, string firstName, string lastName) : Person<ConcretePersonId>(id, firstName, lastName);

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreatePerson()
    {
        // Arrange
        var id = new ConcretePersonId(Guid.NewGuid());
        const string firstName = "John";
        const string lastName = "Doe";

        // Act
        var person = new ConcretePerson(id, firstName, lastName);

        // Assert
        person.Id.Should().Be(id);
        person.FirstName.Should().Be(firstName);
        person.LastName.Should().Be(lastName);
        person.Gender.Should().Be(GenderType.Male); // Default value
    }

    [Theory]
    [InlineData("", "LastName")]
    [InlineData(null, "LastName")]
    [InlineData("FirstName", "")]
    [InlineData("FirstName", null)]
    public void Constructor_WithInvalidNames_ShouldThrowArgumentException(string? firstName, string? lastName)
    {
        // Arrange
        var id = new ConcretePersonId(Guid.NewGuid());

        // Act & Assert
        var act = () => new ConcretePerson(id, firstName!, lastName!);
        act.Should().Throw<NullOrEmptyException>();
    }

    [Fact]
    public void DefaultConstructor_ShouldCreatePersonWithDefaultValues()
    {
        // Arrange & Act
        var person = new ConcretePerson(ConcretePersonId.New(), "Test", "Person");

        // Assert
        person.FirstName.Should().Be("Test");
        person.LastName.Should().Be("Person");
        person.Gender.Should().Be(GenderType.Male);
        person.Country.Should().BeNull();
        person.Photo.Should().BeNull();
        person.LicenseNumber.Should().BeNull();
        person.Email.Should().BeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Gender_SetValue_ShouldReturnCorrectValue()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe")
        {
            // Act
            Gender = GenderType.Female
        };

        // Assert
        person.Gender.Should().Be(GenderType.Female);
    }

    [Fact]
    public void Country_SetValue_ShouldReturnCorrectValue()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe")
        {
            // Act
            Country = Country.France
        };

        // Assert
        person.Country.Should().Be(Country.France);
    }

    [Fact]
    public void Photo_SetValue_ShouldReturnCorrectValue()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");
        var photo = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        person.Photo = photo;

        // Assert
        person.Photo.Should().BeEquivalentTo(photo);
    }

    [Fact]
    public void LicenseNumber_SetValue_ShouldReturnCorrectValue()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");
        const string licenseNumber = "LIC123456";

        // Act
        person.LicenseNumber = licenseNumber;

        // Assert
        person.LicenseNumber.Should().Be(licenseNumber);
    }

    [Fact]
    public void Email_SetValue_ShouldReturnCorrectValue()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");
        const string email = "john.doe@example.com";

        // Act
        person.Email = email;

        // Assert
        person.Email.Should().Be(email);
    }

    #endregion

    #region Rename Method Tests

    [Fact]
    public void Rename_WithValidNames_ShouldUpdateNames()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act
        person.Rename("Jane", "Smith");

        // Assert
        person.FirstName.Should().Be("Jane");
        person.LastName.Should().Be("Smith");
    }

    [Theory]
    [InlineData("", "LastName")]
    [InlineData(null, "LastName")]
    [InlineData("FirstName", "")]
    [InlineData("FirstName", null)]
    public void Rename_WithInvalidNames_ShouldThrowArgumentException(string? firstName, string? lastName)
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act & Assert
        var act = () => person.Rename(firstName!, lastName!);
        act.Should().Throw<NullOrEmptyException>();
    }

    [Fact]
    public void Rename_ShouldNotAffectOtherProperties()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe")
        {
            Gender = GenderType.Female,
            Country = Country.Germany,
            LicenseNumber = "LIC123"
        };

        // Act
        person.Rename("Jane", "Smith");

        // Assert
        person.Gender.Should().Be(GenderType.Female);
        person.Country.Should().Be(Country.Germany);
        person.LicenseNumber.Should().Be("LIC123");
    }

    #endregion

    #region IPerson Interface Tests

    [Fact]
    public void IPerson_Properties_ShouldReturnCorrectValues()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe")
        {
            Gender = GenderType.Female,
            Country = Country.Spain
        };

        // Act & Assert
        person.FirstName.Should().Be("John");
        person.LastName.Should().Be("Doe");
        person.Gender.Should().Be(GenderType.Female);
        person.Country.Should().Be(Country.Spain);
        person.Photo.Should().BeNull();
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnInverseName()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act
        var result = person.ToString();

        // Assert
        result.Should().Be("Doe John");
    }

    [Fact]
    public void ToString_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "Jean-Claude", "Van Damme");

        // Act
        var result = person.ToString();

        // Assert
        result.Should().Be("Van Damme Jean-Claude");
    }

    [Fact]
    public void ToString_WithAccentedCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "François", "Müller");

        // Act
        var result = person.ToString();

        // Assert
        result.Should().Be("Müller François");
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void CompareTo_WithSameInverseName_ShouldReturnZero()
    {
        // Arrange
        var person1 = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");
        var person2 = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act
        var result = person1.CompareTo(person2);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CompareTo_WithDifferentInverseNames_ShouldReturnCorrectOrder()
    {
        // Arrange
        var person1 = new ConcretePerson(ConcretePersonId.New(), "Alice", "Anderson");
        var person2 = new ConcretePerson(ConcretePersonId.New(), "Bob", "Brown");

        // Act
        var result = person1.CompareTo(person2);

        // Assert
        result.Should().BeLessThan(0); // "Anderson Alice" < "Brown Bob"
    }

    [Fact]
    public void CompareTo_WithNull_ShouldReturnPositive()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act
        var result = person.CompareTo(null);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_ShouldBeCaseInsensitive()
    {
        // Arrange
        var person1 = new ConcretePerson(ConcretePersonId.New(), "john", "doe");
        var person2 = new ConcretePerson(ConcretePersonId.New(), "JOHN", "DOE");

        // Act
        var result = person1.CompareTo(person2);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Similarity Tests

    [Fact]
    public void IsSimilar_WithSameInverseName_ShouldReturnTrue()
    {
        // Arrange
        var person1 = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");
        var person2 = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act
        var result = person1.IsSimilar(person2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSimilar_WithDifferentInverseNames_ShouldReturnFalse()
    {
        // Arrange
        var person1 = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");
        var person2 = new ConcretePerson(ConcretePersonId.New(), "Jane", "Smith");

        // Act
        var result = person1.IsSimilar(person2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSimilar_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act
        var result = person.IsSimilar(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSimilar_ShouldBeCaseInsensitive()
    {
        // Arrange
        var person1 = new ConcretePerson(ConcretePersonId.New(), "john", "doe");
        var person2 = new ConcretePerson(ConcretePersonId.New(), "JOHN", "DOE");

        // Act
        var result = person1.IsSimilar(person2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSimilar_WithObjectOverload_ShouldWorkCorrectly()
    {
        // Arrange
        var person1 = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");
        var person2 = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");
        object person2AsObject = person2;

        // Act
        var result = person1.IsSimilar(person2AsObject);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSimilar_WithObjectOverload_NonIPerson_ShouldReturnFalse()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");
        const string notAPerson = "Not a person";

        // Act
        var result = person.IsSimilar(notAPerson);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region PersonExtensions Tests

    [Fact]
    public void GetInverseName_ShouldReturnLastNameFirstName()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act
        var result = person.GetInverseName();

        // Assert
        result.Should().Be("Doe John");
    }

    [Fact]
    public void GetFullName_ShouldReturnFirstNameLastName()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act
        var result = person.GetFullName();

        // Assert
        result.Should().Be("John Doe");
    }

    [Fact]
    public void GetInverseName_WithEmptyNames_ShouldHandleCorrectly()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "A", "B");

        // Act
        var result = person.GetInverseName();

        // Assert
        result.Should().Be("B A");
    }

    [Fact]
    public void GetFullName_WithEmptyNames_ShouldHandleCorrectly()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "A", "B");

        // Act
        var result = person.GetFullName();

        // Assert
        result.Should().Be("A B");
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void Person_ShouldInheritFromAuditableEntity()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act & Assert
        person.Should().BeAssignableTo<AuditableEntity<ConcretePersonId>>();
    }

    [Fact]
    public void Person_ShouldImplementIPerson()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act & Assert
        person.Should().BeAssignableTo<IPerson>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Person_WithVeryLongNames_ShouldHandleCorrectly()
    {
        // Arrange
        var longFirstName = new string('A', 1000);
        var longLastName = new string('B', 1000);

        // Act
        var person = new ConcretePerson(ConcretePersonId.New(), longFirstName, longLastName);

        // Assert
        person.FirstName.Should().Be(longFirstName);
        person.LastName.Should().Be(longLastName);
        person.ToString().Should().Be($"{longLastName} {longFirstName}");
    }

    [Fact]
    public void Person_WithSpecialCharactersInNames_ShouldHandleCorrectly()
    {
        // Arrange
        const string specialFirstName = "Jean-François@#$%";
        const string specialLastName = "O'Connor-Smith";

        // Act
        var person = new ConcretePerson(ConcretePersonId.New(), specialFirstName, specialLastName);

        // Assert
        person.FirstName.Should().Be(specialFirstName);
        person.LastName.Should().Be(specialLastName);
        person.ToString().Should().Be($"{specialLastName} {specialFirstName}");
    }

    [Fact]
    public void Person_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        const string unicodeFirstName = "王小明";
        const string unicodeLastName = "李";

        // Act
        var person = new ConcretePerson(ConcretePersonId.New(), unicodeFirstName, unicodeLastName);

        // Assert
        person.FirstName.Should().Be(unicodeFirstName);
        person.LastName.Should().Be(unicodeLastName);
        person.ToString().Should().Be($"{unicodeLastName} {unicodeFirstName}");
    }

    #endregion

    #region Gender Tests

    [Theory]
    [InlineData(GenderType.Male)]
    [InlineData(GenderType.Female)]
    public void Gender_AllValidValues_ShouldBeSettable(GenderType gender)
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe")
        {
            // Act
            Gender = gender
        };

        // Assert
        person.Gender.Should().Be(gender);
    }

    [Fact]
    public void Gender_DefaultValue_ShouldBeMale()
    {
        // Arrange & Act
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Assert
        person.Gender.Should().Be(GenderType.Male);
    }

    #endregion

    #region Country Tests

    [Fact]
    public void Country_SetToFrance_ShouldReturnFrance()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "Jean", "Dupont")
        {
            // Act
            Country = Country.France
        };

        // Assert
        person.Country.Should().Be(Country.France);
    }

    [Fact]
    public void Country_SetToNull_ShouldReturnNull()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe")
        {
            Country = Country.France
        };

        // Act
        person.Country = null;

        // Assert
        person.Country.Should().BeNull();
    }

    #endregion

    #region Multiple Properties Integration Tests

    [Fact]
    public void Person_WithAllPropertiesSet_ShouldMaintainAllValues()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");
        var photo = new byte[] { 1, 2, 3 };

        // Act
        person.Gender = GenderType.Female;
        person.Country = Country.Canada;
        person.Photo = photo;
        person.LicenseNumber = "LIC789";
        person.Email = "john@example.com";

        // Assert
        person.FirstName.Should().Be("John");
        person.LastName.Should().Be("Doe");
        person.Gender.Should().Be(GenderType.Female);
        person.Country.Should().Be(Country.Canada);
        person.Photo.Should().BeEquivalentTo(photo);
        person.LicenseNumber.Should().Be("LIC789");
        person.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void Person_AfterRename_ShouldKeepOtherProperties()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");
        var photo = new byte[] { 1, 2, 3 };
        person.Gender = GenderType.Female;
        person.Country = Country.Italy;
        person.Photo = photo;
        person.LicenseNumber = "LIC456";
        person.Email = "john@test.com";

        // Act
        person.Rename("Jane", "Smith");

        // Assert
        person.FirstName.Should().Be("Jane");
        person.LastName.Should().Be("Smith");
        person.Gender.Should().Be(GenderType.Female);
        person.Country.Should().Be(Country.Italy);
        person.Photo.Should().BeEquivalentTo(photo);
        person.LicenseNumber.Should().Be("LIC456");
        person.Email.Should().Be("john@test.com");
    }

    #endregion
}

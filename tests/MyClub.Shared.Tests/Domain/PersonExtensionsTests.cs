// -----------------------------------------------------------------------
// <copyright file="PersonExtensionsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using MyClub.Shared.Domain.Extensions;
using MyClub.Shared.Domain.Persons;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities.Geography;
using Xunit;

namespace MyClub.Shared.Tests.Domain;

public class PersonExtensionsTests
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

    #region GetInverseName Tests

    [Fact]
    public void GetInverseName_WithValidNames_ShouldReturnLastNameFirstName()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act
        var result = person.GetInverseName();

        // Assert
        result.Should().Be("Doe John");
    }

    [Fact]
    public void GetInverseName_WithSingleCharacterNames_ShouldReturnCorrectFormat()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "A", "B");

        // Act
        var result = person.GetInverseName();

        // Assert
        result.Should().Be("B A");
    }

    [Fact]
    public void GetInverseName_WithLongNames_ShouldReturnCorrectFormat()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "Jean-Baptiste", "Van Der Berg");

        // Act
        var result = person.GetInverseName();

        // Assert
        result.Should().Be("Van Der Berg Jean-Baptiste");
    }

    [Fact]
    public void GetInverseName_WithNamesContainingSpaces_ShouldPreserveSpaces()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "Mary Jane", "Smith Wilson");

        // Act
        var result = person.GetInverseName();

        // Assert
        result.Should().Be("Smith Wilson Mary Jane");
    }

    [Fact]
    public void GetInverseName_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "José-María", "García-López");

        // Act
        var result = person.GetInverseName();

        // Assert
        result.Should().Be("García-López José-María");
    }

    #endregion

    #region GetFullName Tests

    [Fact]
    public void GetFullName_WithValidNames_ShouldReturnFirstNameLastName()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act
        var result = person.GetFullName();

        // Assert
        result.Should().Be("John Doe");
    }

    [Fact]
    public void GetFullName_WithSingleCharacterNames_ShouldReturnCorrectFormat()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "A", "B");

        // Act
        var result = person.GetFullName();

        // Assert
        result.Should().Be("A B");
    }

    [Fact]
    public void GetFullName_WithLongNames_ShouldReturnCorrectFormat()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "Jean-Baptiste", "Van Der Berg");

        // Act
        var result = person.GetFullName();

        // Assert
        result.Should().Be("Jean-Baptiste Van Der Berg");
    }

    [Fact]
    public void GetFullName_WithNamesContainingSpaces_ShouldPreserveSpaces()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "Mary Jane", "Smith Wilson");

        // Act
        var result = person.GetFullName();

        // Assert
        result.Should().Be("Mary Jane Smith Wilson");
    }

    [Fact]
    public void GetFullName_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "José-María", "García-López");

        // Act
        var result = person.GetFullName();

        // Assert
        result.Should().Be("José-María García-López");
    }

    #endregion

    #region Comparison and Edge Cases Tests

    [Fact]
    public void GetInverseName_And_GetFullName_ShouldBeConsistent()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "John", "Doe");

        // Act
        var inverseName = person.GetInverseName();
        var fullName = person.GetFullName();

        // Assert
        inverseName.Should().Be("Doe John");
        fullName.Should().Be("John Doe");
        inverseName.Should().NotBe(fullName);
    }

    [Fact]
    public void GetInverseName_WithSymmetricalNames_ShouldReturnSameName()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "Bob", "Bob");

        // Act
        var inverseName = person.GetInverseName();
        var fullName = person.GetFullName();

        // Assert
        inverseName.Should().Be("Bob Bob");
        fullName.Should().Be("Bob Bob");
        inverseName.Should().Be(fullName);
    }

    [Theory]
    [InlineData("John", "Doe", "Doe John", "John Doe")]
    [InlineData("Marie", "Curie", "Curie Marie", "Marie Curie")]
    [InlineData("Leonardo", "Da Vinci", "Da Vinci Leonardo", "Leonardo Da Vinci")]
    [InlineData("Van", "Gogh", "Gogh Van", "Van Gogh")]
    public void GetInverseName_And_GetFullName_WithVariousNames_ShouldReturnExpectedFormats(
        string firstName, string lastName, string expectedInverse, string expectedFull)
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), firstName, lastName);

        // Act
        var inverseName = person.GetInverseName();
        var fullName = person.GetFullName();

        // Assert
        inverseName.Should().Be(expectedInverse);
        fullName.Should().Be(expectedFull);
    }

    [Fact]
    public void Extensions_ShouldWorkWithIPersonInterface()
    {
        // Arrange
        IPerson person = new ConcretePerson(ConcretePersonId.New(), "Interface", "Test");

        // Act
        var inverseName = person.GetInverseName();
        var fullName = person.GetFullName();

        // Assert
        inverseName.Should().Be("Test Interface");
        fullName.Should().Be("Interface Test");
    }

    [Fact]
    public void Extensions_ShouldWorkWithPolymorphicPersons()
    {
        // Arrange
        var persons = new IPerson[]
        {
            new ConcretePerson(ConcretePersonId.New(), "Alice", "Anderson"),
            new ConcretePerson(ConcretePersonId.New(), "Bob", "Brown")
        };

        // Act & Assert
        foreach (var person in persons)
        {
            var inverseName = person.GetInverseName();
            var fullName = person.GetFullName();

            inverseName.Should().NotBeNullOrEmpty();
            fullName.Should().NotBeNullOrEmpty();
            inverseName.Should().Contain(person.LastName);
            inverseName.Should().Contain(person.FirstName);
            fullName.Should().Contain(person.FirstName);
            fullName.Should().Contain(person.LastName);
        }
    }

    #endregion

    #region Performance and Stress Tests

    [Fact]
    public void Extensions_WithVeryLongNames_ShouldHandleEfficiently()
    {
        // Arrange
        var longFirstName = new string('A', 1000);
        var longLastName = new string('B', 1000);
        var person = new ConcretePerson(ConcretePersonId.New(), longFirstName, longLastName);

        // Act
        var inverseName = person.GetInverseName();
        var fullName = person.GetFullName();

        // Assert
        inverseName.Should().HaveLength(2001); // 1000 + 1 (space) + 1000
        fullName.Should().HaveLength(2001);
        inverseName.Should().StartWith(longLastName);
        inverseName.Should().EndWith(longFirstName);
        fullName.Should().StartWith(longFirstName);
        fullName.Should().EndWith(longLastName);
    }

    [Fact]
    public void Extensions_MultipleCallsOnSamePerson_ShouldReturnConsistentResults()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "Consistency", "Test");

        // Act
        var inverseName1 = person.GetInverseName();
        var inverseName2 = person.GetInverseName();
        var fullName1 = person.GetFullName();
        var fullName2 = person.GetFullName();

        // Assert
        inverseName1.Should().Be(inverseName2);
        fullName1.Should().Be(fullName2);
    }

    #endregion

    #region Integration Tests with Person Properties

    [Fact]
    public void Extensions_AfterPersonNameChange_ShouldReflectNewNames()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "Original", "Name");
        var originalInverse = person.GetInverseName();
        var originalFull = person.GetFullName();

        // Act
        person.Rename("Modified", "NewName");
        var newInverse = person.GetInverseName();
        var newFull = person.GetFullName();

        // Assert
        originalInverse.Should().Be("Name Original");
        originalFull.Should().Be("Original Name");
        newInverse.Should().Be("NewName Modified");
        newFull.Should().Be("Modified NewName");
        newInverse.Should().NotBe(originalInverse);
        newFull.Should().NotBe(originalFull);
    }

    [Fact]
    public void Extensions_WithPersonHavingOtherProperties_ShouldOnlyUseNames()
    {
        // Arrange
        var person = new ConcretePerson(ConcretePersonId.New(), "Property", "Test")
        {
            Country = Country.France,
            Photo = [1, 2, 3]
        };

        // Act
        var inverseName = person.GetInverseName();
        var fullName = person.GetFullName();

        // Assert
        inverseName.Should().Be("Test Property");
        fullName.Should().Be("Property Test");

        // Extensions should not be affected by other properties
        inverseName.Should().NotContain("France");
        fullName.Should().NotContain("France");
    }

    #endregion
}

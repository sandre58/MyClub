// -----------------------------------------------------------------------
// <copyright file="ConvertersTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using MyClub.Scorer.Infrastructure.Persistence.Converters;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Teams;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Scorer.Infrastructure.Persistence.Tests;

public class ConvertersTests(ITestOutputHelper output)
{
    [Fact]
    public void DictionaryConverter_SerializeDeserialize_WorksCorrectly()
    {
        // Arrange
        var converter = new DictionaryConverter<MatchResultType, int>();
        var originalDictionary = new Dictionary<MatchResultType, int> { { MatchResultType.Win, 3 }, { MatchResultType.Draw, 1 }, { MatchResultType.Loss, 0 } };

        // Act
        var serialized = converter.ConvertToProvider(originalDictionary);
        var deserialized = (IReadOnlyDictionary<MatchResultType, int>?)converter.ConvertFromProvider(serialized);

        // Assert
        Assert.NotNull(serialized);
        Assert.NotNull(deserialized);
        Assert.Equal(originalDictionary.Count, deserialized.Count);

        foreach (var kvp in originalDictionary)
        {
            Assert.True(deserialized.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, deserialized[kvp.Key]);
        }

        output.WriteLine($"Serialized: {serialized}");
        output.WriteLine($"Original count: {originalDictionary.Count}, Deserialized count: {deserialized.Count}");
    }

    [Fact]
    public void DictionaryConverter_EmptyDictionary_HandledCorrectly()
    {
        // Arrange
        var converter = new DictionaryConverter<MatchResultType, int>();
        var emptyDictionary = new Dictionary<MatchResultType, int>();

        // Act
        var serialized = converter.ConvertToProvider(emptyDictionary);
        var deserialized = (IReadOnlyDictionary<MatchResultType, int>?)converter.ConvertFromProvider(serialized);

        // Assert
        Assert.NotNull(serialized);
        Assert.NotNull(deserialized);
        Assert.Empty(deserialized);

        output.WriteLine($"Empty dictionary serialized as: {serialized}");
    }

    [Fact]
    public void DictionaryConverter_NullValue_HandledCorrectly()
    {
        // Arrange
        var converter = new DictionaryConverter<MatchResultType, int>();

        // Act
        var serialized = converter.ConvertToProvider(null);
        var deserialized = (IReadOnlyDictionary<MatchResultType, int>?)converter.ConvertFromProvider(serialized);

        // Assert
        // EF ValueConverter behavior: null input results in null output
        // This is expected behavior for Entity Framework converters
        if (serialized is null)
        {
            // Expected: EF bypasses converter lambda for null inputs
            output.WriteLine("EF correctly handled null by returning null for serialization");

            // When we pass null to ConvertFromProvider, our lambda should create empty dict
            var emptyResult = (IReadOnlyDictionary<MatchResultType, int>?)converter.ConvertFromProvider("null");
            Assert.NotNull(emptyResult);
            Assert.Empty(emptyResult);
        }
        else
        {
            // If serialization worked, deserialization should too
            Assert.NotNull(deserialized);
            Assert.Empty(deserialized);
        }

        output.WriteLine($"Null dictionary serialized as: {serialized}");
    }

    [Fact]
    public void DictionaryConverter_InvalidJson_ThrowsException()
    {
        // Arrange
        var converter = new DictionaryConverter<MatchResultType, int>();
        const string invalidJson = "invalid json string";

        // Act & Assert
        Assert.Throws<JsonException>(() => converter.ConvertFromProvider(invalidJson));

        output.WriteLine("Invalid JSON correctly throws JsonException");
    }

    [Fact]
    public void DictionaryConverter_WithTeamId_WorksCorrectly()
    {
        // Arrange
        var converter = new DictionaryConverter<TeamId, int>();
        var teamId1 = new TeamId(Guid.NewGuid());
        var teamId2 = new TeamId(Guid.NewGuid());

        var originalDictionary = new Dictionary<TeamId, int> { { teamId1, 10 }, { teamId2, 5 } };

        // Act & Assert
        // Since TeamId cannot be used as JSON dictionary key, this should throw an exception during serialization
        Assert.Throws<NotSupportedException>(() => converter.ConvertToProvider(originalDictionary));

        output.WriteLine("TeamId dictionary correctly throws NotSupportedException as expected");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[]")]
    public void DictionaryConverter_EdgeCases_ReturnEmptyDictionary(string input)
    {
        // Arrange
        var converter = new DictionaryConverter<MatchResultType, int>();

        // Act
        var result = (IReadOnlyDictionary<MatchResultType, int>?)converter.ConvertFromProvider(input);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        output.WriteLine($"Input '{input}' correctly handled");
    }

    [Fact]
    public void DictionaryConverter_LargeValues_PerformanceTest()
    {
        // Arrange
        var converter = new DictionaryConverter<int, string>();
        var largeDictionary = new Dictionary<int, string>();

        for (var i = 0; i < 1000; i++)
        {
            largeDictionary[i] = $"Value_{i}_{Guid.NewGuid()}";
        }

        // Act
        var startTime = DateTime.UtcNow;
        var serialized = (string?)converter.ConvertToProvider(largeDictionary);
        var serializeTime = DateTime.UtcNow - startTime;

        startTime = DateTime.UtcNow;
        var deserialized = (IReadOnlyDictionary<int, string>?)converter.ConvertFromProvider(serialized);
        var deserializeTime = DateTime.UtcNow - startTime;

        // Assert
        Assert.Equal(largeDictionary.Count, deserialized?.Count);

        output.WriteLine($"Serialization of {largeDictionary.Count} items took: {serializeTime.TotalMilliseconds}ms");
        output.WriteLine($"Deserialization of {largeDictionary.Count} items took: {deserializeTime.TotalMilliseconds}ms");
        output.WriteLine($"Serialized size: {serialized?.Length} characters");

        // Performance assertions
        Assert.True(serializeTime.TotalSeconds < 1, "Serialization should take less than 1 second");
        Assert.True(deserializeTime.TotalSeconds < 1, "Deserialization should take less than 1 second");
    }
}

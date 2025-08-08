// -----------------------------------------------------------------------
// <copyright file="DictionaryConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MyClub.Shared.Infrastructure.Persistence.Converters;

/// <summary>
/// Entity Framework Core value converter for generic dictionary types, enabling storage of complex
/// key-value pairs as JSON in the database. This converter provides type-safe serialization and
/// deserialization of dictionary objects with comprehensive null and empty value handling.
/// </summary>
/// <typeparam name="TKey">The key type for the dictionary. Must be a non-null reference type.</typeparam>
/// <typeparam name="TValue">The value type for the dictionary.</typeparam>
/// <remarks>
/// The DictionaryConverter provides robust persistence capabilities for domain objects that require
/// dictionary storage, such as configuration mappings, scoring rules, and complex domain value objects.
/// The converter handles various edge cases including null values, empty dictionaries, and invalid JSON,
/// ensuring reliable database operations and proper fallback behavior.
/// </remarks>
public sealed class DictionaryConverter<TKey, TValue>() : ValueConverter<IReadOnlyDictionary<TKey, TValue>, string>(

    // Convert dictionary to JSON string for database storage
    static v => JsonSerializer.Serialize(v, v.GetType(), ConverterHelper.SerializerOptions),

    // Convert JSON string back to dictionary with comprehensive null handling
    static v => string.IsNullOrWhiteSpace(v) || v == "null" || v == "[]"
        ? new Dictionary<TKey, TValue>().AsReadOnly()
        : JsonSerializer.Deserialize<IReadOnlyDictionary<TKey, TValue>>(v, ConverterHelper.CreateDeserializerOptions<DictionaryJsonConverter<TKey, TValue>>()) ?? new Dictionary<TKey, TValue>().AsReadOnly())
    where TKey : notnull;

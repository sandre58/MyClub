// -----------------------------------------------------------------------
// <copyright file="DictionaryJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyClub.Shared.Infrastructure.Persistence.Converters;

/// <summary>
/// System.Text.Json converter for generic dictionary types, providing specialized JSON
/// serialization and deserialization with robust error handling and infinite recursion
/// prevention for Entity Framework Core value converter scenarios.
/// </summary>
/// <typeparam name="TKey">The key type for the dictionary. Must be a non-null reference type.</typeparam>
/// <typeparam name="TValue">The value type for the dictionary.</typeparam>
/// <remarks>
/// The DictionaryJsonConverter provides robust JSON conversion for dictionary objects with
/// comprehensive error handling and recursion prevention. The converter ensures reliable
/// operation in Entity Framework Core scenarios where JSON conversion may encounter various
/// edge cases including malformed JSON, null values, and converter recursion issues.
/// </remarks>
internal sealed class DictionaryJsonConverter<TKey, TValue> : JsonConverter<IReadOnlyDictionary<TKey, TValue>>
    where TKey : notnull
{
    /// <summary>
    /// Reads JSON data and converts it to a read-only dictionary with comprehensive error
    /// handling and recursion prevention to ensure reliable deserialization in database scenarios.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the dictionary data.</param>
    /// <param name="typeToConvert">The target type for conversion (IReadOnlyDictionary).</param>
    /// <param name="options">JSON serializer options for the conversion operation.</param>
    /// <returns>A read-only dictionary populated with the JSON data, or empty dictionary on errors.</returns>
    /// <remarks>
    /// The deserialization process includes sophisticated error handling and recursion prevention
    /// by creating modified serializer options that exclude this converter instance. This approach
    /// prevents infinite recursion while ensuring reliable conversion even with malformed or
    /// unexpected JSON input data commonly encountered in database storage scenarios.
    /// </remarks>
    public override IReadOnlyDictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            // Create new options without this converter to avoid infinite recursion
            var newOptions = new JsonSerializerOptions(options);

            // Remove this converter to prevent infinite recursion
            for (var i = newOptions.Converters.Count - 1; i >= 0; i--)
            {
                if (newOptions.Converters[i] is DictionaryJsonConverter<TKey, TValue>)
                    newOptions.Converters.RemoveAt(i);
            }

            var result = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(ref reader, newOptions);
            return result?.AsReadOnly() ?? new Dictionary<TKey, TValue>().AsReadOnly();
        }
        catch (JsonException)
        {
            // Return empty dictionary for any JSON parsing errors (invalid JSON, empty strings, etc.)
            return new Dictionary<TKey, TValue>().AsReadOnly();
        }
    }

    /// <summary>
    /// Writes a read-only dictionary to JSON using polymorphic serialization to preserve
    /// the complete dictionary structure and content for reliable database storage.
    /// </summary>
    /// <param name="writer">The JSON writer for outputting the serialized data.</param>
    /// <param name="value">The dictionary instance to serialize.</param>
    /// <param name="options">JSON serializer options for the serialization operation.</param>
    /// <remarks>
    /// The serialization process uses polymorphic serialization to ensure complete fidelity
    /// of the dictionary structure and contents, enabling accurate reconstruction during
    /// subsequent deserialization operations while maintaining optimal JSON formatting.
    /// </remarks>
    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<TKey, TValue> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}

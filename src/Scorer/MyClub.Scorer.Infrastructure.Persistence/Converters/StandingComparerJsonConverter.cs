// -----------------------------------------------------------------------
// <copyright file="StandingComparerJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.Shared.Domain.Standings.Comparers;
using MyNet.Utilities;

namespace MyClub.Scorer.Infrastructure.Persistence.Converters;

/// <summary>
/// System.Text.Json converter for StandingComparer objects, providing specialized JSON
/// serialization and deserialization for complex standing comparison logic using reflection-based
/// type reconstruction and robust error handling for unknown or missing comparer types.
/// </summary>
/// <remarks>
/// The StandingComparerJsonConverter handles the sophisticated comparison logic used in league
/// standing calculations, supporting dynamic reconstruction of comparer chains from stored type
/// names while providing fallback behavior for unknown types and graceful error recovery to
/// ensure reliable standing calculation even with evolving comparer implementations.
/// </remarks>
internal sealed class StandingComparerJsonConverter : JsonConverter<StandingComparer>
{
    /// <summary>
    /// Reads JSON data containing comparer type names and reconstructs the appropriate
    /// StandingComparer instance using reflection-based type resolution with comprehensive
    /// error handling and fallback to default comparison logic.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the comparer data.</param>
    /// <param name="typeToConvert">The target type for conversion (StandingComparer).</param>
    /// <param name="options">JSON serializer options for the conversion operation.</param>
    /// <returns>A StandingComparer instance with the reconstructed comparison logic, or default comparer on errors.</returns>
    /// <remarks>
    /// The deserialization process uses reflection to dynamically instantiate comparer types from
    /// their stored names, providing robust error handling to ensure standing calculations continue
    /// to function even when encountering unknown or outdated comparer type references in the database.
    /// </remarks>
    public override StandingComparer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            var comparerNames = JsonSerializer.Deserialize<List<string>>(ref reader, options);

            if (comparerNames == null || comparerNames.Count == 0)
                return StandingComparer.Default;

            var comparers = comparerNames.Select(static name => CreateFromTypeName(name)).NotNull().ToList();

            return comparers.Count > 0
                ? new(comparers)
                : StandingComparer.Default;
        }
        catch
        {
            return StandingComparer.Default;
        }
    }

    /// <summary>
    /// Writes a StandingComparer object to JSON by serializing the type names of all
    /// constituent comparison components for reliable reconstruction during deserialization.
    /// </summary>
    /// <param name="writer">The JSON writer for outputting the serialized data.</param>
    /// <param name="value">The StandingComparer instance to serialize.</param>
    /// <param name="options">JSON serializer options for the serialization operation.</param>
    /// <remarks>
    /// The serialization process extracts type names from all comparison components to create
    /// a compact but complete representation that enables accurate reconstruction of the
    /// comparison logic while maintaining compatibility with future comparer implementations.
    /// </remarks>
    public override void Write(Utf8JsonWriter writer, StandingComparer value, JsonSerializerOptions options)
    {
        var comparerNames = value.Select(static x => x.GetType().Name).ToList();
        JsonSerializer.Serialize(writer, comparerNames, options);
    }

    private static IStandingComparer? CreateFromTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        var assembly = typeof(StandingComparer).Assembly;
        var foundType = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName && typeof(IStandingComparer).IsAssignableFrom(t));

        return foundType != null ? (IStandingComparer?)Activator.CreateInstance(foundType) : null;
    }
}

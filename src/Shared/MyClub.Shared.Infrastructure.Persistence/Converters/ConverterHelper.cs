// -----------------------------------------------------------------------
// <copyright file="ConverterHelper.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyClub.Shared.Infrastructure.Persistence.Converters;

/// <summary>
/// Static helper class providing standardized JSON serialization options and utilities
/// for Entity Framework Core value converters. This class ensures consistent JSON
/// serialization behavior across all converters in the persistence layer.
/// </summary>
/// <remarks>
/// The ConverterHelper centralizes JSON serialization configuration to maintain consistency
/// across all value converters, providing optimized settings for database storage scenarios
/// and standardized deserialization options with custom converter support.
/// </remarks>
public static class ConverterHelper
{
    /// <summary>
    /// Gets the standardized JSON serializer options for converting objects to database strings.
    /// Configured for compact serialization optimized for database storage.
    /// </summary>
    /// <remarks>
    /// These options are optimized for database storage with minimal size and consistent formatting,
    /// using compact JSON without indentation to reduce storage overhead while maintaining readability.
    /// </remarks>
    public static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    /// <summary>
    /// Creates JSON deserializer options with a custom converter for type-specific deserialization.
    /// This method enables specialized deserialization behavior for complex domain objects.
    /// </summary>
    /// <typeparam name="T">The JsonConverter type to use for custom deserialization.</typeparam>
    /// <returns>JsonSerializerOptions configured with the specified custom converter.</returns>
    /// <remarks>
    /// This method provides a standardized way to create deserializer options with custom converters,
    /// ensuring consistent configuration for case-insensitive property matching and proper handling
    /// of complex domain objects that require specialized JSON deserialization logic.
    /// </remarks>
    public static JsonSerializerOptions CreateDeserializerOptions<T>()
        where T : JsonConverter
        => new() { Converters = { Activator.CreateInstance<T>() }, PropertyNameCaseInsensitive = true };
}

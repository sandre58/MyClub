// -----------------------------------------------------------------------
// <copyright file="ListConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MyClub.Shared.Infrastructure.Persistence.Converters;

/// <summary>
/// Entity Framework Core value converter for generic list types, enabling storage of collections
/// as delimited strings in the database. This converter provides flexible serialization with
/// customizable delimiter and conversion functions for various data types.
/// </summary>
/// <typeparam name="T">The element type for the list.</typeparam>
/// <param name="toString">Optional custom function to convert elements to strings.</param>
/// <param name="fromString">Optional custom function to convert strings back to elements.</param>
/// <param name="delimiter">Character used to separate list elements in the database string.</param>
/// <remarks>
/// The ListConverter provides efficient storage for simple collections that don't require
/// the complexity of JSON serialization, using delimited string format for better database
/// compatibility and query performance. Includes built-in type conversion for common types
/// and supports custom conversion functions for domain-specific requirements.
/// </remarks>
public class ListConverter<T>(Func<T, string>? toString = null, Func<string, T>? fromString = null, char delimiter = ';') : ValueConverter<IReadOnlyCollection<T>, string>(list => ConvertToString(list, delimiter, toString), str => ConvertFromString(str, delimiter, fromString))
{
    private static string ConvertToString(IReadOnlyCollection<T> list, char delimiter, Func<T, string>? toString)
    {
        if (list.Count == 0)
            return string.Empty;

        var converter = toString ?? (static item => item?.ToString() ?? string.Empty);
        return string.Join(delimiter.ToString(), list.Select(converter));
    }

    private static List<T> ConvertFromString(string str, char delimiter, Func<string, T>? fromString)
    {
        if (string.IsNullOrWhiteSpace(str))
            return [];

        var converter = fromString ?? CreateDefaultConverter();

        return [.. str.Split(delimiter, StringSplitOptions.RemoveEmptyEntries).Select(s => converter(s.Trim()))];
    }

    private static Func<string, T> CreateDefaultConverter()
    {
        var type = typeof(T);
        return typeof(T) switch
        {
            _ when type.IsEnum => s => (T)Enum.Parse(type, s),
            _ when type == typeof(string) => s => (T)(object)s,
            _ when type == typeof(int) => s => (T)(object)int.Parse(s, CultureInfo.InvariantCulture),
            _ when type == typeof(Guid) => s => (T)(object)Guid.Parse(s, CultureInfo.InvariantCulture),
            _ when type == typeof(DateTime) => s => (T)(object)DateTime.Parse(s, CultureInfo.InvariantCulture),
            _ => s => (T)Convert.ChangeType(s, typeof(T), CultureInfo.InvariantCulture)
        };
    }
}

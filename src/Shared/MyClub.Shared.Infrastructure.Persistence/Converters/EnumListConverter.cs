// -----------------------------------------------------------------------
// <copyright file="EnumListConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MyClub.Shared.Infrastructure.Persistence.Converters;

/// <summary>
/// Entity Framework Core value converter for collections of enum values, enabling storage of
/// enum lists as delimited strings in the database. This converter provides type-safe enum
/// serialization with efficient string-based storage for enumeration collections.
/// </summary>
/// <typeparam name="TEnum">The enum type for the collection elements.</typeparam>
/// <param name="delimiter">Character used to separate enum values in the database string.</param>
/// <remarks>
/// The EnumListConverter specializes collection storage for enum types, providing efficient
/// string-based persistence while maintaining type safety and proper enum value validation
/// during serialization and deserialization operations.
/// </remarks>
public class EnumListConverter<TEnum>(char delimiter = ',') : ValueConverter<IReadOnlyCollection<TEnum>, string>(collection => ConvertToString(collection, delimiter), str => ConvertFromString(str, delimiter))
    where TEnum : struct, Enum
{
    private static string ConvertToString(IReadOnlyCollection<TEnum> collection, char delimiter) => collection.Count == 0
            ? string.Empty
            : string.Join(delimiter.ToString(), collection.Select(static e => e.ToString()));

    private static ReadOnlyCollection<TEnum> ConvertFromString(string str, char delimiter) => string.IsNullOrWhiteSpace(str)
            ? new List<TEnum>().AsReadOnly()
            : str.Split(delimiter, StringSplitOptions.RemoveEmptyEntries)
                  .Select(static s => Enum.Parse<TEnum>(s.Trim()))
                  .ToList()
                  .AsReadOnly();
}

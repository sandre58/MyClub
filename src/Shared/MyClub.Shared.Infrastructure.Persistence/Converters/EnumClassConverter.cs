// -----------------------------------------------------------------------
// <copyright file="EnumClassConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyNet.Utilities;

namespace MyClub.Shared.Infrastructure.Persistence.Converters;

/// <summary>
/// Entity Framework Core value converter for EnumClass types, enabling storage of enumeration
/// class objects as strings in the database. This converter provides type-safe conversion
/// between domain enumeration classes and their string representations.
/// </summary>
/// <typeparam name="TEnum">The EnumClass type to convert.</typeparam>
/// <remarks>
/// The EnumClassConverter handles persistence of sophisticated enumeration classes that provide
/// richer functionality than standard enums, converting between domain enum class instances
/// and their string names while maintaining type safety and proper error handling.
/// </remarks>
public sealed class EnumClassConverter<TEnum>() : ValueConverter<TEnum, string>(static x => x.ToString(CultureInfo.InvariantCulture), static x => ConvertToEnum(x))
    where TEnum : EnumClass<TEnum>
{
    private static TEnum ConvertToEnum(string? value) => !string.IsNullOrEmpty(value) && EnumClass<TEnum>.TryFromName(value, out var result) ? result! : throw new InvalidCastException(nameof(value));
}

/// <summary>
/// Entity Framework Core value converter for nullable EnumClass types, providing safe conversion
/// with proper null handling between optional domain enumeration classes and their string
/// representations for database storage.
/// </summary>
/// <typeparam name="TEnum">The EnumClass type to convert.</typeparam>
/// <remarks>
/// The NullableEnumClassConverter extends the EnumClass conversion pattern to support optional
/// enumeration values while maintaining type safety and proper null value handling in database
/// operations for scenarios where enumeration values may be absent.
/// </remarks>
public sealed class NullableEnumClassConverter<TEnum>() : ValueConverter<TEnum?, string?>(static x => x == null ? null : x.ToString(CultureInfo.InvariantCulture), static x => ConvertToEnum(x))
    where TEnum : EnumClass<TEnum>
{
    private static TEnum? ConvertToEnum(string? value) => !string.IsNullOrEmpty(value) && EnumClass<TEnum>.TryFromName(value, out var result) ? result : null;
}

// -----------------------------------------------------------------------
// <copyright file="RoundFormatConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.Scorer.Domain.RoundAggregate.Format;
using MyClub.Shared.Infrastructure.Persistence.Converters;

namespace MyClub.Scorer.Infrastructure.Persistence.Converters;

/// <summary>
/// Entity Framework Core value converter for RoundFormat objects, enabling storage of polymorphic
/// round format configurations as JSON in the database. This converter supports all round format
/// types including single elimination, home-and-away, best-of series, and replay formats.
/// </summary>
/// <remarks>
/// The RoundFormatConverter handles the persistence of complex round format objects that define
/// tournament round behavior, supporting polymorphic serialization to maintain type information
/// and enable proper reconstruction of format-specific settings and rules.
/// </remarks>
internal sealed class RoundFormatConverter() : ValueConverter<RoundFormat, string>(static v => JsonSerializer.Serialize(v, v.GetType(), ConverterHelper.SerializerOptions),
    static v => JsonSerializer.Deserialize<RoundFormat>(v, ConverterHelper.CreateDeserializerOptions<RoundFormatJsonConverter>())!);

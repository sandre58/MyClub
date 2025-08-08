// -----------------------------------------------------------------------
// <copyright file="StandingComparerConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.Shared.Domain.Standings.Comparers;
using MyClub.Shared.Infrastructure.Persistence.Converters;

namespace MyClub.Scorer.Infrastructure.Persistence.Converters;

/// <summary>
/// Entity Framework Core value converter for StandingComparer objects, enabling storage of complex
/// standing comparison logic as JSON in the database. This converter supports polymorphic
/// comparison strategies including points-based, goal difference, and head-to-head comparisons.
/// </summary>
/// <remarks>
/// The StandingComparerConverter handles the persistence of sophisticated standing comparison
/// algorithms used to rank teams in league tables, supporting various comparison strategies
/// and maintaining type information for proper reconstruction of comparison logic.
/// </remarks>
internal sealed class StandingComparerConverter() : ValueConverter<StandingComparer, string>(static v => JsonSerializer.Serialize(v, v.GetType(), ConverterHelper.SerializerOptions),
    static v => JsonSerializer.Deserialize<StandingComparer>(v, ConverterHelper.CreateDeserializerOptions<StandingComparerJsonConverter>())!);

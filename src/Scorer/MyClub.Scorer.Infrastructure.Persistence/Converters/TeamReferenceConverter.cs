// -----------------------------------------------------------------------
// <copyright file="TeamReferenceConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Infrastructure.Persistence.Converters;

namespace MyClub.Scorer.Infrastructure.Persistence.Converters;

/// <summary>
/// Entity Framework Core value converter for polymorphic TeamReference objects,
/// enabling storage of both concrete and virtual team references as JSON in the database.
/// This converter handles the complexity of tournament bracket systems where teams
/// may be determined dynamically based on match results or group standings.
/// </summary>
internal sealed class TeamReferenceConverter() : ValueConverter<TeamReference, string>(

    // Convert TeamReference to JSON string for database storage
    static v => JsonSerializer.Serialize(v, v.GetType(), ConverterHelper.SerializerOptions),

    // Convert JSON string back to appropriate TeamReference subtype
    static v => JsonSerializer.Deserialize<TeamReference>(v, ConverterHelper.CreateDeserializerOptions<TeamReferenceJsonConverter>())!);

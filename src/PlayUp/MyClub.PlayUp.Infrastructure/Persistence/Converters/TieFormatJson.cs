// -----------------------------------------------------------------------
// <copyright file="TieFormatJson.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Deterministic System.Text.Json serialization for <see cref="TieFormat"/> as JSONB.
/// </summary>
internal static class TieFormatJson
{
    internal static readonly JsonSerializerOptions Options = RegulationJson.Options;

    internal static string Serialize(TieFormat tieFormat) => JsonSerializer.Serialize(tieFormat, Options);

    internal static TieFormat Deserialize(string json) =>
        JsonSerializer.Deserialize<TieFormat>(json, Options)
        ?? throw new InvalidOperationException("TieFormat JSON is empty.");
}

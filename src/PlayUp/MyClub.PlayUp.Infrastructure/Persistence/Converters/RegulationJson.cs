// -----------------------------------------------------------------------
// <copyright file="RegulationJson.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Deterministic System.Text.Json options for persisting <see cref="Regulation"/> as JSONB.
/// </summary>
internal static class RegulationJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static string Serialize(Regulation regulation) => JsonSerializer.Serialize(regulation, Options);

    internal static Regulation Deserialize(string json) =>
        JsonSerializer.Deserialize<Regulation>(json, Options)
        ?? throw new InvalidOperationException("Regulation JSON is empty.");
}

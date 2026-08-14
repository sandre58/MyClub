// -----------------------------------------------------------------------
// <copyright file="StageRegulationJson.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Deterministic System.Text.Json options for persisting <see cref="StageRegulation"/> as JSONB.
/// </summary>
internal static class StageRegulationJson
{
    internal static readonly JsonSerializerOptions Options = RegulationJson.Options;

    internal static string Serialize(StageRegulation regulation) => JsonSerializer.Serialize(regulation, Options);

    internal static StageRegulation Deserialize(string json) =>
        JsonSerializer.Deserialize<StageRegulation>(json, Options)
        ?? throw new InvalidOperationException("StageRegulation JSON is empty.");
}

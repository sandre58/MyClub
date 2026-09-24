// -----------------------------------------------------------------------
// <copyright file="PlacementAwardPathJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists <see cref="PlacementAwardPath"/> with structural <c>SourcePairKey</c> only.
/// </summary>
internal sealed class PlacementAwardPathJsonConverter : JsonConverter<PlacementAwardPath>
{
    public override PlacementAwardPath Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var outcome = JsonSerializer.Deserialize<ProgressionOutcome>(
            root.GetProperty("Outcome").GetRawText(),
            options);
        var rank = root.GetProperty("Rank").GetInt32();

        if (!root.TryGetProperty("SourcePairKey", out var pairElement)
            || pairElement.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(pairElement.GetString()))
        {
            throw new JsonException("PlacementAwardPath requires SourcePairKey.");
        }

        return new PlacementAwardPath(pairElement.GetString()!, outcome, rank);
    }

    public override void Write(
        Utf8JsonWriter writer,
        PlacementAwardPath value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("SourcePairKey", value.SourcePairKey);
        writer.WritePropertyName("Outcome");
        JsonSerializer.Serialize(writer, value.Outcome, options);
        writer.WriteNumber("Rank", value.Rank);
        writer.WriteEndObject();
    }
}

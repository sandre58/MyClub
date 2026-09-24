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
/// Persists <see cref="PlacementAwardPath"/> with <c>SourcePairKey</c>.
/// Dual-reads legacy <c>SourceFixtureId</c> as interim Guid N key.
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

        string pairKey;
        if (root.TryGetProperty("SourcePairKey", out var pairElement)
            && pairElement.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(pairElement.GetString()))
        {
            pairKey = pairElement.GetString()!;
        }
        else if (root.TryGetProperty("SourceFixtureId", out var fixtureElement))
        {
            var fixtureId = JsonSerializer.Deserialize<Guid>(fixtureElement.GetRawText(), options);
            pairKey = fixtureId.ToString("N");
        }
        else
        {
            throw new JsonException("PlacementAwardPath requires SourcePairKey (or legacy SourceFixtureId).");
        }

        return new PlacementAwardPath(pairKey, outcome, rank);
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

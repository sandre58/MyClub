// -----------------------------------------------------------------------
// <copyright file="ProgressionPathJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists <see cref="ProgressionPath"/> with structural <c>SourcePairKey</c> only.
/// </summary>
internal sealed class ProgressionPathJsonConverter : JsonConverter<ProgressionPath>
{
    public override ProgressionPath Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var outcome = JsonSerializer.Deserialize<ProgressionOutcome>(
            root.GetProperty("Outcome").GetRawText(),
            options);
        var destination = JsonSerializer.Deserialize<ProgressionDestination>(
            root.GetProperty("Destination").GetRawText(),
            options)
            ?? throw new JsonException("ProgressionPath.Destination is required.");

        if (!root.TryGetProperty("SourcePairKey", out var pairElement)
            || pairElement.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(pairElement.GetString()))
        {
            throw new JsonException("ProgressionPath requires SourcePairKey.");
        }

        return new ProgressionPath(pairElement.GetString()!, outcome, destination);
    }

    public override void Write(
        Utf8JsonWriter writer,
        ProgressionPath value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("SourcePairKey", value.SourcePairKey);
        writer.WritePropertyName("Outcome");
        JsonSerializer.Serialize(writer, value.Outcome, options);
        writer.WritePropertyName("Destination");
        JsonSerializer.Serialize(writer, value.Destination, options);
        writer.WriteEndObject();
    }
}

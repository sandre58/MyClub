// -----------------------------------------------------------------------
// <copyright file="ProgressionRulesJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists <see cref="ProgressionRules"/> as intents (authoring SoT) + path projection for Apply.
/// Paths-only JSON (no Intents) still deserializes as path-list authoring.
/// </summary>
internal sealed class ProgressionRulesJsonConverter : JsonConverter<ProgressionRules>
{
    public override ProgressionRules Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var paths = root.TryGetProperty("Paths", out var pathsElement)
            ? JsonSerializer.Deserialize<List<ProgressionPath>>(pathsElement.GetRawText(), options) ?? []
            : [];

        if (!root.TryGetProperty("Intents", out var intentsElement))
        {
            return new ProgressionRules(paths);
        }

        var intents =
            JsonSerializer.Deserialize<List<ProgressionIntent>>(intentsElement.GetRawText(), options)
            ?? [];
        return intents.Count > 0
            ? ProgressionRules.FromPersisted(intents, paths)
            : new ProgressionRules(paths);
    }

    public override void Write(
        Utf8JsonWriter writer,
        ProgressionRules value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.Intents.Count > 0)
        {
            writer.WritePropertyName("Intents");
            JsonSerializer.Serialize(writer, value.Intents, options);
        }

        writer.WritePropertyName("Paths");
        JsonSerializer.Serialize(writer, value.Paths, options);
        writer.WriteEndObject();
    }
}

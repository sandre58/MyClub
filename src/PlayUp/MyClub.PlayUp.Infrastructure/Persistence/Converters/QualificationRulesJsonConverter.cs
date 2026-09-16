// -----------------------------------------------------------------------
// <copyright file="QualificationRulesJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists <see cref="QualificationRules"/> as intents (authoring SoT) + path projection for Apply.
/// Legacy JSON with only <c>Paths</c> still deserializes via path migration.
/// </summary>
internal sealed class QualificationRulesJsonConverter : JsonConverter<QualificationRules>
{
    public override QualificationRules Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var paths = root.TryGetProperty("Paths", out var pathsElement)
            ? JsonSerializer.Deserialize<List<QualificationPath>>(pathsElement.GetRawText(), options) ?? []
            : [];

        if (!root.TryGetProperty("Intents", out var intentsElement)) return new QualificationRules(paths);
        var intents =
            JsonSerializer.Deserialize<List<QualificationIntent>>(intentsElement.GetRawText(), options)
            ?? [];
        return intents.Count > 0 ? QualificationRules.FromPersisted(intents, paths) : new QualificationRules(paths);
    }

    public override void Write(
        Utf8JsonWriter writer,
        QualificationRules value,
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

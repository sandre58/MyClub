// -----------------------------------------------------------------------
// <copyright file="QualificationConditionJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Explicit JSON mapping for <see cref="QualificationCondition"/> using Domain properties and factory.
/// </summary>
/// <remarks>
/// Domain public shape: <see cref="QualificationCondition.MinimumPoints"/>.
/// Rehydration uses <see cref="QualificationCondition.PointsAtLeast"/> only — never the private ctor.
/// </remarks>
internal sealed class QualificationConditionJsonConverter : JsonConverter<QualificationCondition>
{
    private const string MinimumPointsProperty = nameof(QualificationCondition.MinimumPoints);

    /// <inheritdoc />
    public override QualificationCondition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("QualificationCondition JSON must be an object.");
        }

        int? minimumPoints = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in QualificationCondition JSON.");
            }

            var propertyName = reader.GetString();
            reader.Read();

            if (propertyName == MinimumPointsProperty)
            {
                minimumPoints = reader.GetInt32();
            }
            else
            {
                reader.Skip();
            }
        }

        return QualificationCondition.PointsAtLeast(
            minimumPoints
            ?? throw new JsonException($"QualificationCondition JSON requires '{MinimumPointsProperty}'."));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, QualificationCondition value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(MinimumPointsProperty, value.MinimumPoints);
        writer.WriteEndObject();
    }
}

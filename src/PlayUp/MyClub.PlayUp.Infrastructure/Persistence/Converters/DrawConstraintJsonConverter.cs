// -----------------------------------------------------------------------
// <copyright file="DrawConstraintJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Explicit JSON mapping for <see cref="DrawConstraint"/> using Domain properties and public factories/ctors.
/// </summary>
/// <remarks>
/// Domain public shape: <see cref="DrawConstraint.ConstraintType"/>, <see cref="DrawConstraint.Enforcement"/>,
/// <see cref="DrawConstraint.MaxPerGroup"/>. Rehydration uses
/// <see cref="DrawConstraint.MaxSameAssociationPerGroup"/> or the public ctor — never private ctors.
/// </remarks>
internal sealed class DrawConstraintJsonConverter : JsonConverter<DrawConstraint>
{
    private const string ConstraintTypeProperty = nameof(DrawConstraint.ConstraintType);
    private const string EnforcementProperty = nameof(DrawConstraint.Enforcement);
    private const string MaxPerGroupProperty = nameof(DrawConstraint.MaxPerGroup);

    /// <inheritdoc />
    public override DrawConstraint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("DrawConstraint JSON must be an object.");
        }

        DrawConstraintType? constraintType = null;
        ConstraintEnforcement? enforcement = null;
        int? maxPerGroup = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in DrawConstraint JSON.");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case ConstraintTypeProperty:
                    constraintType = JsonSerializer.Deserialize<DrawConstraintType>(ref reader, options);
                    break;
                case EnforcementProperty:
                    enforcement = JsonSerializer.Deserialize<ConstraintEnforcement>(ref reader, options);
                    break;
                case MaxPerGroupProperty:
                    maxPerGroup = reader.TokenType == JsonTokenType.Null
                        ? null
                        : reader.GetInt32();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        if (constraintType is null)
        {
            throw new JsonException($"DrawConstraint JSON requires '{ConstraintTypeProperty}'.");
        }

        var resolvedEnforcement = enforcement ?? ConstraintEnforcement.Preferred;

        return constraintType == DrawConstraintType.MaxSameAssociationPerGroup
            ? DrawConstraint.MaxSameAssociationPerGroup(
                maxPerGroup
                ?? throw new JsonException(
                    $"DrawConstraint JSON requires '{MaxPerGroupProperty}' for MaxSameAssociationPerGroup."),
                resolvedEnforcement)
            : new DrawConstraint(constraintType.Value, resolvedEnforcement);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DrawConstraint value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(ConstraintTypeProperty);
        JsonSerializer.Serialize(writer, value.ConstraintType, options);
        writer.WritePropertyName(EnforcementProperty);
        JsonSerializer.Serialize(writer, value.Enforcement, options);

        if (value.MaxPerGroup is { } maxPerGroup)
        {
            writer.WriteNumber(MaxPerGroupProperty, maxPerGroup);
        }

        writer.WriteEndObject();
    }
}

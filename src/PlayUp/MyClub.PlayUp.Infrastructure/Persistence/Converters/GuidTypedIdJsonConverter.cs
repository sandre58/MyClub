// -----------------------------------------------------------------------
// <copyright file="GuidTypedIdJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Explicit JSON mapping for Guid-backed typed ids used inside StageRegulation jsonb graphs.
/// </summary>
/// <remarks>
/// Domain public shape is a single <c>Value</c> Guid with a public <c>new TId(Guid value)</c> constructor.
/// STJ cannot reliably rehydrate get-only readonly record structs, so Infra writes/reads that contract explicitly.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="GuidTypedIdJsonConverter{TId}"/> class.
/// </remarks>
/// <param name="create">Domain constructor / factory from Guid.</param>
/// <param name="readValue">Reads <c>Value</c> from the typed id.</param>
internal sealed class GuidTypedIdJsonConverter<TId>(Func<Guid, TId> create, Func<TId, Guid> readValue) : JsonConverter<TId>
    where TId : struct
{
    private const string ValueProperty = "Value";

    /// <inheritdoc />
    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return create(reader.GetGuid());
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"{typeof(TId).Name} JSON must be a Guid string or an object with '{ValueProperty}'.");
        }

        Guid? value = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected property name in {typeof(TId).Name} JSON.");
            }

            var propertyName = reader.GetString();
            reader.Read();

            if (propertyName == ValueProperty)
            {
                value = reader.GetGuid();
            }
            else
            {
                reader.Skip();
            }
        }

        return create(
            value ?? throw new JsonException($"{typeof(TId).Name} JSON requires '{ValueProperty}'."));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(ValueProperty, readValue(value));
        writer.WriteEndObject();
    }
}

// -----------------------------------------------------------------------
// <copyright file="RoundFormatJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate.Format;

namespace MyClub.Scorer.Infrastructure.Persistence.Converters;

/// <summary>
/// System.Text.Json converter for polymorphic RoundFormat objects, providing specialized
/// JSON serialization and deserialization for the complex round format hierarchy used
/// in tournament management and knockout competition scenarios.
/// </summary>
/// <remarks>
/// The RoundFormatJsonConverter handles the sophisticated polymorphic nature of round formats,
/// supporting conversion between JSON and various RoundFormat subtypes including single elimination,
/// home-and-away, best-of series, and replay formats. The converter uses type-based discrimination
/// to accurately reconstruct the appropriate RoundFormat subtype while preserving all format-specific
/// configuration details and timing parameters for reliable tournament management.
/// </remarks>
internal sealed class RoundFormatJsonConverter : JsonConverter<RoundFormat>
{
    /// <summary>
    /// Reads JSON data and converts it to the appropriate RoundFormat subtype based on the
    /// type discriminator and format-specific properties present in the JSON object.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the RoundFormat data.</param>
    /// <param name="typeToConvert">The target type for conversion (RoundFormat).</param>
    /// <param name="options">JSON serializer options for the conversion operation.</param>
    /// <returns>The appropriate RoundFormat subtype instance populated with the JSON configuration data.</returns>
    /// <remarks>
    /// The deserialization process uses type-based discrimination combined with property parsing
    /// to reconstruct complex round format configurations including timing parameters, match rules,
    /// and format-specific settings while ensuring all tournament format requirements are preserved.
    /// </remarks>
    public override RoundFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Get Type
        if (!root.TryGetProperty(nameof(RoundFormat.Type), out var jsonType))
        {
            throw new JsonException("Cannot determine RoundFormat type.");
        }

        // Deserialize RegulationTime
        var regulationTime = PeriodFormat.Default;
        if (root.TryGetProperty(nameof(RoundFormat.RegulationTime), out var regulationTimeElement))
        {
            regulationTime = DeserializePeriodFormat(regulationTimeElement);
        }

        // Deserialize ExtraTime
        PeriodFormat? extraTime = null;
        if (root.TryGetProperty(nameof(RoundFormat.ExtraTime), out var extraTimeElement))
        {
            extraTime = DeserializePeriodFormat(extraTimeElement);
        }

        // Get NumberOfPenaltyShootouts
        int? penaltyShootouts = null;
        if (root.TryGetProperty(nameof(RoundFormat.NumberOfPenaltyShootouts), out var jsonNumberOfPenaltyShootouts))
        {
            penaltyShootouts = jsonNumberOfPenaltyShootouts.GetInt32();
        }

        var type = Enum.Parse<RoundFormatType>(jsonType.GetString()!);

        return type switch
        {
            RoundFormatType.Single => new SingleFormat(regulationTime, extraTime, penaltyShootouts),
            RoundFormatType.Replay => new ReplayFormat(regulationTime, extraTime, penaltyShootouts),
            RoundFormatType.BestOf => DeserializeBestOfFormat(root, regulationTime, extraTime, penaltyShootouts),
            RoundFormatType.HomeAndAway => DeserializeHomeAndAwayFormat(root, regulationTime, extraTime, penaltyShootouts),
            _ => throw new JsonException($"Unknown RoundFormat type: {type}")
        };
    }

    private static PeriodFormat DeserializePeriodFormat(JsonElement element)
    {
        var number = element.TryGetProperty(nameof(PeriodFormat.Number), out var numberElement) ? numberElement.GetInt32() : 2;

        var duration = element.TryGetProperty(nameof(PeriodFormat.Duration), out var durationElement) ? TimeSpan.Parse(durationElement.GetString()!, CultureInfo.InvariantCulture) : TimeSpan.FromMinutes(45);

        TimeSpan? halfTimeDuration = null;
        if (element.TryGetProperty(nameof(PeriodFormat.HalfTimeDuration), out var halfTimeElement))
        {
            halfTimeDuration = TimeSpan.Parse(halfTimeElement.GetString()!, CultureInfo.InvariantCulture);
        }

        return new(number, duration, halfTimeDuration);
    }

    private static BestOfFormat DeserializeBestOfFormat(JsonElement root, PeriodFormat regulationTime, PeriodFormat? extraTime, int? penaltyShootouts)
    {
        var maxGames = 1;
        if (root.TryGetProperty(nameof(BestOfFormat.MaxGames), out var jsonMaxGames))
        {
            maxGames = jsonMaxGames.GetInt32();
        }

        // Deserialize InvertTeamsByStage array
        var invertTeamsByStage = Array.Empty<bool>();
        if (!root.TryGetProperty(nameof(BestOfFormat.InvertTeamsByStage), out var jsonInvertArray))
            return new(maxGames, invertTeamsByStage, regulationTime, extraTime, penaltyShootouts);

        var arrayLength = jsonInvertArray.GetArrayLength();
        invertTeamsByStage = new bool[arrayLength];
        for (var i = 0; i < arrayLength; i++)
        {
            invertTeamsByStage[i] = jsonInvertArray[i].GetBoolean();
        }

        return new(maxGames, invertTeamsByStage, regulationTime, extraTime, penaltyShootouts);
    }

    private static HomeAndAwayFormat DeserializeHomeAndAwayFormat(JsonElement root, PeriodFormat regulationTime, PeriodFormat? extraTime, int? penaltyShootouts)
    {
        var useAwayGoals = false;
        if (root.TryGetProperty(nameof(HomeAndAwayFormat.UseAwayGoals), out var jsonUseAwayGoals))
        {
            useAwayGoals = jsonUseAwayGoals.GetBoolean();
        }

        return new(regulationTime, extraTime, useAwayGoals, penaltyShootouts);
    }

    /// <summary>
    /// Writes a RoundFormat object to JSON using polymorphic serialization to preserve
    /// the exact type information and all properties of the specific RoundFormat subtype.
    /// </summary>
    /// <param name="writer">The JSON writer for outputting the serialized data.</param>
    /// <param name="value">The RoundFormat instance to serialize.</param>
    /// <param name="options">JSON serializer options for the serialization operation.</param>
    /// <remarks>
    /// The serialization process uses polymorphic serialization to ensure complete preservation
    /// of round format configuration details and type-specific properties for accurate
    /// reconstruction during subsequent deserialization operations.
    /// </remarks>
    public override void Write(Utf8JsonWriter writer, RoundFormat value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}

// -----------------------------------------------------------------------
// <copyright file="TeamReferenceJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Infrastructure.Persistence.Converters;

/// <summary>
/// System.Text.Json converter for polymorphic TeamReference objects, providing specialized
/// JSON serialization and deserialization for the complex team reference hierarchy used
/// in tournament bracket management and team progression scenarios.
/// </summary>
/// <remarks>
/// The TeamReferenceJsonConverter handles the complex polymorphic nature of team references,
/// supporting conversion between JSON and various TeamReference subtypes including concrete
/// teams and virtual team references that enable sophisticated tournament bracket structures.
/// The converter uses property-based type discrimination to accurately reconstruct the
/// appropriate TeamReference subtype during deserialization while maintaining type information
/// during serialization for reliable round-trip conversion.
/// </remarks>
internal sealed class TeamReferenceJsonConverter : JsonConverter<TeamReference>
{
    /// <summary>
    /// Reads JSON data and converts it to the appropriate TeamReference subtype based on
    /// the properties present in the JSON object. Uses property-based discrimination to
    /// determine the correct concrete type for instantiation.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the TeamReference data.</param>
    /// <param name="typeToConvert">The target type for conversion (TeamReference).</param>
    /// <param name="options">JSON serializer options for the conversion operation.</param>
    /// <returns>The appropriate TeamReference subtype instance populated with the JSON data.</returns>
    /// <remarks>
    /// The deserialization process uses a property-based discrimination strategy to identify
    /// the correct TeamReference subtype by examining the presence of specific properties
    /// in the JSON object. The converter supports all major TeamReference types and provides
    /// appropriate fallback behavior for unknown or malformed JSON structures.
    /// </remarks>
    public override TeamReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // ConcreteTeamReference
        if (root.TryGetProperty(nameof(ConcreteTeamReference.Id), out var teamId))
        {
            return TeamId.From(Guid.Parse(teamId.GetString()!)).ToReference();
        }

        // FixtureResultReference
        if (root.TryGetProperty(nameof(FixtureResultReference.RoundId), out var roundId)
            && root.TryGetProperty(nameof(FixtureResultReference.FixtureId), out var fixtureId))
        {
            var result = root.TryGetProperty(nameof(FixtureResultReference.Type), out var typeJson) ? Enum.Parse<VirtualTeamType>(typeJson.GetString()!) : default;
            return new FixtureResultReference(RoundId.From(Guid.Parse(roundId.GetString()!)), FixtureId.From(Guid.Parse(fixtureId.GetString()!)), result);
        }

        // GroupRankReference
        if (root.TryGetProperty(nameof(GroupRankReference.StageId), out var stageId)
            && root.TryGetProperty(nameof(GroupRankReference.GroupId), out var groupId))
        {
            var groupRank = root.TryGetProperty(nameof(GroupRankReference.Rank), out var groupRankJson) ? groupRankJson.GetInt32() : 1;
            return new GroupRankReference(StageId.From(Guid.Parse(stageId.GetString()!)), GroupId.From(Guid.Parse(groupId.GetString()!)), groupRank);
        }

        // ChampionshipRankReference
        if (!root.TryGetProperty(nameof(ChampionshipRankReference.StageId), out var championshipId))
            throw new JsonException("Cannot determine TeamReference type.");

        var rank = root.TryGetProperty(nameof(ChampionshipRankReference.Rank), out var rankJson) ? rankJson.GetInt32() : 1;
        return new ChampionshipRankReference(StageId.From(Guid.Parse(championshipId.GetString()!)), rank);
    }

    /// <summary>
    /// Writes a TeamReference object to JSON, using polymorphic serialization to preserve
    /// the exact type information and all properties of the specific TeamReference subtype.
    /// </summary>
    /// <param name="writer">The JSON writer for outputting the serialized data.</param>
    /// <param name="value">The TeamReference instance to serialize.</param>
    /// <param name="options">JSON serializer options for the serialization operation.</param>
    /// <remarks>
    /// The serialization process uses polymorphic serialization to ensure that all properties
    /// specific to the actual TeamReference subtype are included in the JSON output, enabling
    /// accurate reconstruction during deserialization while maintaining complete type fidelity.
    /// </remarks>
    public override void Write(Utf8JsonWriter writer, TeamReference value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}

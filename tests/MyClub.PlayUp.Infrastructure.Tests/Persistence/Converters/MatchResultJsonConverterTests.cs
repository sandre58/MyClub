// -----------------------------------------------------------------------
// <copyright file="MatchResultJsonConverterTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence.Converters;

public sealed class MatchResultJsonConverterTests
{
    private readonly MatchResultJsonConverter _converter = new();

    [Fact]
    public void Convert_round_trips_null()
    {
        var json = _converter.ConvertToProvider(null);
        var restored = _converter.ConvertFromProvider(json);

        json.Should().BeNull();
        restored.Should().BeNull();
    }

    [Fact]
    public void Convert_round_trips_played_score_without_extra_time_or_shootout()
    {
        var result = new MatchResult(ResultType.Played, new Score(2, 1));

        var json = _converter.ConvertToProvider(result);
        var restored = _converter.ConvertFromProvider(json);

        restored.Should().Be(result);
        json.Should().BeOfType<string>().Which.Should().NotContain("PenaltyShootoutScore");
    }

    [Fact]
    public void Convert_round_trips_extra_time_without_shootout()
    {
        var result = new MatchResult(ResultType.Played, new Score(1, 0), extraTimePlayed: true);

        var json = _converter.ConvertToProvider(result);
        var restored = (MatchResult?)_converter.ConvertFromProvider(json);

        restored.Should().Be(result);
        restored.ExtraTimePlayed.Should().BeTrue();
        restored.PenaltyShootoutScore.Should().BeNull();
    }

    [Fact]
    public void Convert_round_trips_equal_score_with_decisive_shootout()
    {
        var result = new MatchResult(
            ResultType.Played,
            new Score(1, 1),
            extraTimePlayed: true,
            penaltyShootoutScore: new PenaltyShootoutScore(5, 4));

        var json = _converter.ConvertToProvider(result);
        var restored = (MatchResult?)_converter.ConvertFromProvider(json);

        restored.Should().Be(result);
        restored.PenaltyShootoutScore.Should().Be(new PenaltyShootoutScore(5, 4));
    }

    [Theory]
    [InlineData(ResultType.Played)]
    [InlineData(ResultType.Forfeit)]
    [InlineData(ResultType.WalkOver)]
    [InlineData(ResultType.Administrative)]
    public void Convert_round_trips_all_result_types(ResultType type)
    {
        var result = new MatchResult(type, new Score(3, 0));

        _converter.ConvertFromProvider(_converter.ConvertToProvider(result)).Should().Be(result);
    }

    [Fact]
    public void Convert_uses_pascal_case_and_numeric_enums()
    {
        var json = _converter.ConvertToProvider(new MatchResult(ResultType.Played, new Score(2, 1)))
            .Should()
            .BeOfType<string>()
            .Subject;

        json.Should().Contain("\"Type\":0");
        json.Should().Contain("\"HomeGoals\":2");
        json.Should().Contain("\"AwayGoals\":1");
        json.Should().NotContain("\"Played\"");
        json.Should().NotContain("\"Score\"");
    }

    [Fact]
    public void Comparer_uses_record_equality_and_independent_snapshot()
    {
        var left = new MatchResult(ResultType.Played, new Score(0, 0), penaltyShootoutScore: new PenaltyShootoutScore(4, 3));
        var right = new MatchResult(ResultType.Played, new Score(0, 0), penaltyShootoutScore: new PenaltyShootoutScore(4, 3));
        var comparer = MatchResultJsonConverter.Comparer;

        comparer.Equals(left, right).Should().BeTrue();
        comparer.Equals(left, null).Should().BeFalse();
        comparer.Equals(null, null).Should().BeTrue();
        comparer.GetHashCode(left).Should().Be(comparer.GetHashCode(right));

        var snapshot = comparer.Snapshot(left);
        snapshot.Should().Be(left);
        ReferenceEquals(snapshot, left).Should().BeFalse();
    }

    [Fact]
    public void Convert_ordinary_played_result_omits_confrontation_tie_flag_from_json_shape()
    {
        var result = new MatchResult(ResultType.Played, new Score(2, 1));

        var json = _converter.ConvertToProvider(result).Should().BeOfType<string>().Subject;

        json.Should().Be("{\"Type\":0,\"HomeGoals\":2,\"AwayGoals\":1,\"ExtraTimePlayed\":false}");
        json.Should().NotContain("ConfrontationTiePenaltyShootout");
    }

    [Fact]
    public void Convert_legacy_json_without_confrontation_tie_flag_round_trips()
    {
        const string legacy = "{\"Type\":0,\"HomeGoals\":2,\"AwayGoals\":1,\"ExtraTimePlayed\":false}";

        var restored = (MatchResult?)_converter.ConvertFromProvider(legacy);

        restored.Should().Be(new MatchResult(ResultType.Played, new Score(2, 1)));
        restored!.ConfrontationTiePenaltyShootout.Should().BeFalse();
    }

    [Fact]
    public void Convert_confrontation_tie_result_serializes_flag()
    {
        const string incoming =
            "{\"Type\":0,\"HomeGoals\":1,\"AwayGoals\":0,\"ExtraTimePlayed\":false,"
            + "\"PenaltyHomeGoals\":5,\"PenaltyAwayGoals\":4,\"ConfrontationTiePenaltyShootout\":true}";
        var result = (MatchResult?)_converter.ConvertFromProvider(incoming);
        result.Should().NotBeNull();
        result!.ConfrontationTiePenaltyShootout.Should().BeTrue();

        var json = _converter.ConvertToProvider(result).Should().BeOfType<string>().Subject;

        json.Should().Contain("\"ConfrontationTiePenaltyShootout\":true");
        json.Should().Contain("\"PenaltyHomeGoals\":5");
        json.Should().Contain("\"PenaltyAwayGoals\":4");
        _converter.ConvertFromProvider(json).Should().Be(result);
    }

    [Fact]
    public void Comparer_snapshots_confrontation_tie_flag()
    {
        const string leftJson =
            "{\"Type\":0,\"HomeGoals\":1,\"AwayGoals\":0,\"ExtraTimePlayed\":false,"
            + "\"PenaltyHomeGoals\":5,\"PenaltyAwayGoals\":4,\"ConfrontationTiePenaltyShootout\":true}";
        const string rightJson =
            "{\"Type\":0,\"HomeGoals\":1,\"AwayGoals\":0,\"ExtraTimePlayed\":true,"
            + "\"PenaltyHomeGoals\":4,\"PenaltyAwayGoals\":3,\"ConfrontationTiePenaltyShootout\":true}";
        var left = (MatchResult)_converter.ConvertFromProvider(leftJson)!;
        var right = (MatchResult)_converter.ConvertFromProvider(rightJson)!;
        var comparer = MatchResultJsonConverter.Comparer;

        comparer.Equals(left, right).Should().BeFalse();
        comparer.Snapshot(left).Should().Be(left);
        comparer.Snapshot(left)!.ConfrontationTiePenaltyShootout.Should().BeTrue();
    }

    [Fact]
    public void Convert_rejects_confrontation_tie_flag_without_shootout_scores()
    {
        const string json =
            "{\"Type\":0,\"HomeGoals\":1,\"AwayGoals\":0,\"ExtraTimePlayed\":false,\"ConfrontationTiePenaltyShootout\":true}";

        var act = () => _converter.ConvertFromProvider(json);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void Convert_rejects_confrontation_tie_flag_with_equal_play_score_and_shootout()
    {
        const string json =
            "{\"Type\":0,\"HomeGoals\":0,\"AwayGoals\":0,\"ExtraTimePlayed\":false,"
            + "\"PenaltyHomeGoals\":5,\"PenaltyAwayGoals\":4,\"ConfrontationTiePenaltyShootout\":true}";

        var act = () => _converter.ConvertFromProvider(json);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void Convert_rejects_confrontation_tie_flag_when_type_is_not_played()
    {
        const string json =
            "{\"Type\":1,\"HomeGoals\":1,\"AwayGoals\":0,\"ExtraTimePlayed\":false,"
            + "\"PenaltyHomeGoals\":5,\"PenaltyAwayGoals\":4,\"ConfrontationTiePenaltyShootout\":true}";

        var act = () => _converter.ConvertFromProvider(json);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }
}

// -----------------------------------------------------------------------
// <copyright file="RegulationJsonConverterTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence.Converters;

public sealed class RegulationJsonConverterTests
{
    private readonly RegulationJsonConverter _converter = new();

    [Fact]
    public void Convert_round_trips_standard_regulation()
    {
        var regulation = SampleRegulations.Standard();

        var json = _converter.ConvertToProvider(regulation);
        var restored = _converter.ConvertFromProvider(json);

        restored.Should().Be(regulation);
        json.Should().BeOfType<string>().Which.Should().NotContain("ExtraTimePolicy");
        json.Should().BeOfType<string>().Which.Should().NotContain("PenaltyShootoutPolicy");
    }

    [Fact]
    public void Convert_round_trips_optional_match_policies()
    {
        var regulation = SampleRegulations.WithExtraTimeAndShootout();

        var json = _converter.ConvertToProvider(regulation);
        var restored = _converter.ConvertFromProvider(json);

        restored.Should().Be(regulation);
        json.Should().BeOfType<string>().Which.Should().Contain("ExtraTimePolicy");
        json.Should().BeOfType<string>().Which.Should().Contain("PenaltyShootoutPolicy");
    }

    [Fact]
    public void Convert_uses_pascal_case_and_numeric_enums()
    {
        var json = _converter.ConvertToProvider(SampleRegulations.Standard()).Should().BeOfType<string>().Subject;

        json.Should().Contain("\"EntryRules\"");
        json.Should().Contain("\"RankingCriteria\":[0,1,2,5]");
        json.Should().Contain("\"DisciplinaryRules\"");
        json.Should().NotContain("\"GoalDifference\"");
    }

    [Fact]
    public void Convert_round_trips_disciplinary_allowed_types()
    {
        var regulation = new Regulation(
            SampleRegulations.Standard().EntryRules,
            SampleRegulations.Standard().MatchRules,
            SampleRegulations.Standard().StandingRules,
            new DisciplinaryRules([DisciplinaryType.Yellow, DisciplinaryType.White, DisciplinaryType.Red]));

        var json = _converter.ConvertToProvider(regulation);
        var restored = _converter.ConvertFromProvider(json);

        restored.Should().Be(regulation);
        restored.Should().BeOfType<Regulation>().Which.DisciplinaryRules.Allows(DisciplinaryType.White).Should().BeTrue();
    }

    [Fact]
    public void Convert_defaults_missing_disciplinary_rules_to_none()
    {
        const string legacyJson =
            """
            {"EntryRules":{"MinimumTeams":2,"MaximumTeams":64},"MatchRules":{"Duration":{"DurationPerPeriod":45,"NumberOfPeriods":2,"HalfTimeDuration":15},"AdministrativeResultPolicy":{"ForfeitWinnerGoals":3,"ForfeitLoserGoals":0},"ExtraTimePolicy":null,"PenaltyShootoutPolicy":null},"StandingRules":{"Points":{"WinPoints":3,"DrawPoints":1,"LossPoints":0},"RankingCriteria":[0,1,2,5]}}
            """;

        var restored = _converter.ConvertFromProvider(legacyJson);

        restored.Should().BeOfType<Regulation>().Which.DisciplinaryRules.Should().Be(DisciplinaryRules.None);
    }
}

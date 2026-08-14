// -----------------------------------------------------------------------
// <copyright file="RegulationJsonConverterTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
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
        json.Should().NotContain("\"GoalDifference\"");
    }
}

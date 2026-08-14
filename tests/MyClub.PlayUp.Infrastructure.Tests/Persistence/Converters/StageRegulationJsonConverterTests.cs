// -----------------------------------------------------------------------
// <copyright file="StageRegulationJsonConverterTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence.Converters;

public sealed class StageRegulationJsonConverterTests
{
    private readonly StageRegulationJsonConverter _converter = new();
    private readonly TieFormatJsonConverter _tieFormatConverter = new();

    [Fact]
    public void StageRegulation_convert_round_trips()
    {
        var regulation = StageRegulation.MaterializeFrom(SampleRegulations.WithExtraTimeAndShootout())
            .WithTieFormat(new TieFormat(2, true, new AwayGoalsRule()));

        var json = _converter.ConvertToProvider(regulation);
        var restored = _converter.ConvertFromProvider(json);

        restored.Should().Be(regulation);
    }

    [Fact]
    public void TieFormat_convert_round_trips_and_null()
    {
        var tieFormat = new TieFormat(1, false, extraTimeRule: new ExtraTimeRule(), penaltyShootoutRule: new PenaltyShootoutRule());

        _tieFormatConverter.ConvertFromProvider(_tieFormatConverter.ConvertToProvider(tieFormat)).Should().Be(tieFormat);
        _tieFormatConverter.ConvertFromProvider(_tieFormatConverter.ConvertToProvider(null)).Should().BeNull();
    }
}

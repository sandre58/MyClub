// -----------------------------------------------------------------------
// <copyright file="AffectationAuthoringHistoricalBackfillTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Infrastructure.Persistence;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class AffectationAuthoringHistoricalBackfillTests
{
    [Fact]
    public void ShouldCopy_when_no_inbound_destination()
    {
        var rootId = Guid.NewGuid();
        var peerId = Guid.NewGuid();
        var others = new List<(Guid, string)>
        {
            (peerId, """{"ProgressionRules":null}""")
        };

        AffectationAuthoringHistoricalBackfill
            .ShouldCopyCompositionToAffectation(rootId, others)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void ShouldNotCopy_when_inbound_DestinationStageId_present()
    {
        var targetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var regulationJson =
            "{\"ProgressionRules\":{\"Intents\":[{\"DestinationStageId\":\"" + targetId + "\"}]}}";
        var others = new List<(Guid, string)>
        {
            (sourceId, regulationJson)
        };

        AffectationAuthoringHistoricalBackfill
            .ShouldCopyCompositionToAffectation(targetId, others)
            .Should()
            .BeFalse();
    }
}

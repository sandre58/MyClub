// -----------------------------------------------------------------------
// <copyright file="TypedIdTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Common;

public sealed class TypedIdTests
{
    [Fact]
    public void New_creates_a_Guid_version_7_identifier()
    {
        // Act
        var id = CompetitionId.New();

        // Assert
        id.Value.Should().NotBe(Guid.Empty);
        id.Value.Version.Should().Be(7);
    }

    [Fact]
    public void Constructing_a_typed_id_with_empty_guid_throws_DomainException()
    {
        // Act
        var act = () => new CompetitionId(Guid.Empty);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*CompetitionId*empty*");
    }

    [Fact]
    public void All_typed_ids_reject_empty_guid()
    {
        // Arrange
        var factories = new (Func<Guid, object> Factory, string Name)[]
        {
            (v => new CompetitionId(v), nameof(CompetitionId)),
            (v => new StageId(v), nameof(StageId)),
            (v => new MatchId(v), nameof(MatchId)),
            (v => new EntryId(v), nameof(EntryId)),
            (v => new DrawId(v), nameof(DrawId)),
            (v => new PenaltyId(v), nameof(PenaltyId)),
            (v => new GroupId(v), nameof(GroupId)),
            (v => new RoundId(v), nameof(RoundId)),
            (v => new FixtureId(v), nameof(FixtureId)),
            (v => new MatchdayId(v), nameof(MatchdayId)),
            (v => new TeamId(v), nameof(TeamId)),
            (v => new MemberId(v), nameof(MemberId))
        };

        // Act / Assert
        foreach (var (factory, name) in factories)
        {
            var act = () => factory(Guid.Empty);
            act.Should().Throw<DomainException>()
                .WithMessage($"*{name}*empty*");
        }
    }

    [Fact]
    public void All_typed_ids_New_produce_version_7_guids()
    {
        // Arrange
        var factories = new Func<object>[]
        {
            () => CompetitionId.New(),
            () => StageId.New(),
            () => MatchId.New(),
            () => EntryId.New(),
            () => DrawId.New(),
            () => PenaltyId.New(),
            () => GroupId.New(),
            () => RoundId.New(),
            () => FixtureId.New(),
            () => MatchdayId.New(),
            () => TeamId.New(),
            () => MemberId.New()
        };

        // Act / Assert
        foreach (var factory in factories)
        {
            var id = factory();
            var value = (Guid)id.GetType().GetProperty("Value")!.GetValue(id)!;
            value.Version.Should().Be(7);
        }
    }
}

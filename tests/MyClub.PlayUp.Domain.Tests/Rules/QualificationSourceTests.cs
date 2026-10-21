// -----------------------------------------------------------------------
// <copyright file="QualificationSourceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class QualificationSourceTests
{
    [Fact]
    public void AcrossGroups_source_requires_position_ge_1()
    {
        var actZero = () => QualificationSource.AcrossGroups(0);
        var actNegative = () => QualificationSource.AcrossGroups(-1);

        actZero.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
        actNegative.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void AcrossGroups_rejects_groupId()
    {
        var act = () => new QualificationSource(
            RankingScope.AcrossGroups,
            GroupId.New(),
            acrossGroupsPosition: 3);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void AcrossGroups_factory_sets_scope_and_position()
    {
        var source = QualificationSource.AcrossGroups(3);

        source.Scope.Should().Be(RankingScope.AcrossGroups);
        source.GroupId.Should().BeNull();
        source.AcrossGroupsPosition.Should().Be(3);
    }

    [Fact]
    public void AcrossGroups_rejects_missing_position()
    {
        var act = () => new QualificationSource(RankingScope.AcrossGroups);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Group_rejects_across_groups_position()
    {
        var act = () => new QualificationSource(
            RankingScope.Group,
            GroupId.New(),
            acrossGroupsPosition: 3);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Overall_rejects_across_groups_position()
    {
        var act = () => new QualificationSource(
            RankingScope.Overall,
            groupId: null,
            acrossGroupsPosition: 3);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Equality_includes_across_groups_position()
    {
        var left = QualificationSource.AcrossGroups(3);
        var right = QualificationSource.AcrossGroups(3);
        var other = QualificationSource.AcrossGroups(2);

        left.Should().Be(right);
        left.Should().NotBe(other);
    }
}

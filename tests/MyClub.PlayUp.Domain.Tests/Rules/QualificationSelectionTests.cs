// -----------------------------------------------------------------------
// <copyright file="QualificationSelectionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class QualificationSelectionTests
{
    [Fact]
    public void Range_accepts_inclusive_bounds()
    {
        var selection = new QualificationSelection(SelectionMode.Range, 9, 24);

        selection.Mode.Should().Be(SelectionMode.Range);
        selection.Value.Should().Be(9);
        selection.EndValue.Should().Be(24);
    }

    [Fact]
    public void Range_rejects_missing_end_value()
    {
        var act = () => new QualificationSelection(SelectionMode.Range, 9);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Range_rejects_end_before_start()
    {
        var act = () => new QualificationSelection(SelectionMode.Range, 10, 5);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Range_rejects_value_below_one()
    {
        var act = () => new QualificationSelection(SelectionMode.Range, 0, 10);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Non_range_rejects_end_value()
    {
        var act = () => new QualificationSelection(SelectionMode.Top, 8, 10);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Position_still_requires_value_at_least_one()
    {
        var act = () => new QualificationSelection(SelectionMode.Position, 0);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }
}

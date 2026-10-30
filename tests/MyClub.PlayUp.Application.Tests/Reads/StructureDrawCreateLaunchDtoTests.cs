// -----------------------------------------------------------------------
// <copyright file="StructureDrawCreateLaunchDtoTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureDrawCreateLaunchDtoTests
{
    [Fact]
    public void Allow_sets_allowed_without_block_reason()
    {
        var dto = new StructureDrawCreateLaunchDto(true, null);

        dto.Allowed.Should().BeTrue();
        dto.BlockReasonCode.Should().BeNull();
    }

    [Fact]
    public void Deny_requires_block_reason_code()
    {
        var dto = new StructureDrawCreateLaunchDto(false, "DrawRulesMissing");

        dto.Allowed.Should().BeFalse();
        dto.BlockReasonCode.Should().Be("DrawRulesMissing");
    }

    [Fact]
    public void Deny_without_block_reason_code_throws()
    {
        var act = () => new StructureDrawCreateLaunchDto(false, null);

        act.Should().Throw<ArgumentException>().WithParameterName("blockReasonCode");
    }

    [Fact]
    public void Allow_with_block_reason_code_throws()
    {
        var act = () => new StructureDrawCreateLaunchDto(true, "DrawRulesMissing");

        act.Should().Throw<ArgumentException>().WithParameterName("blockReasonCode");
    }

    [Fact]
    public void Default_struct_value_is_not_a_valid_denial_and_cannot_be_reconstructed()
    {
        var unset = default(StructureDrawCreateLaunchDto);

        unset.Allowed.Should().BeFalse();
        unset.BlockReasonCode.Should().BeNull();

        var act = () => new StructureDrawCreateLaunchDto(unset.Allowed, unset.BlockReasonCode);

        act.Should().Throw<ArgumentException>().WithParameterName("blockReasonCode");
    }
}

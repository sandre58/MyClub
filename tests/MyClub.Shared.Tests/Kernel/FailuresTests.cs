// -----------------------------------------------------------------------
// <copyright file="FailuresTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.Shared.Kernel.Results;
using Xunit;

namespace MyClub.Shared.Tests.Kernel;

public class FailuresTests
{
    [Fact]
    public void AlreadyExists_ShouldReturnFailureResultWithCorrectMessageAndCode()
    {
        const string identifier = "TestId";
        var result = Failures.AlreadyExists<string>(identifier);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("Errors.AlreadyExists");
        result.ErrorMessage.Should().Be($"'{identifier}' already exists.");
        result.ErrorArgs.Should().Contain(identifier);
        result.Value.Should().Be(null);
    }

    [Fact]
    public void NameAlreadyExists_ShouldReturnFailureResultWithCorrectMessageAndCode()
    {
        const string name = "TestName";
        var result = Failures.NameAlreadyExists<string>(name);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("Errors.NameAlreadyExists");
        result.ErrorMessage.Should().Be($"An item with name '{name}' already exists.");
        result.ErrorArgs.Should().Contain(name);
        result.Value.Should().Be(null);
    }

    [Fact]
    public void NotFoundT_ShouldReturnFailureResultWithCorrectMessageAndCode()
    {
        const string identifier = "TestId";
        var result = Failures.NotFound<string>(identifier);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("Errors.NotFound");
        result.ErrorMessage.Should().Be($"Item '{identifier}' not found.");
        result.ErrorArgs.Should().Contain(identifier);
        result.Value.Should().Be(null);
    }

    [Fact]
    public void NotFound_ShouldReturnFailureResultWithCorrectMessageAndCode()
    {
        const string identifier = "TestId";
        var result = Failures.NotFound(identifier);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("Errors.NotFound");
        result.ErrorMessage.Should().Be($"Item '{identifier}' not found.");
        result.ErrorArgs.Should().Contain(identifier);
    }

    [Fact]
    public void GetErrorCode_ShouldReturnCorrectErrorCode()
    {
        // Test indirect via public methods
        Failures.AlreadyExists<string>("id").ErrorCode.Should().Be("Errors.AlreadyExists");
        Failures.NameAlreadyExists<string>("name").ErrorCode.Should().Be("Errors.NameAlreadyExists");
        Failures.NotFound<string>("id").ErrorCode.Should().Be("Errors.NotFound");
        Failures.NotFound("id").ErrorCode.Should().Be("Errors.NotFound");
    }
}

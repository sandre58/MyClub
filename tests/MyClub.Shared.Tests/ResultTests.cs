// -----------------------------------------------------------------------
// <copyright file="ResultTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using MyClub.Shared.Kernel.Results;
using MyClub.Tests.Common;
using Xunit;

namespace MyClub.Shared.Tests;

public class ResultTests : TestBase
{
    [Fact]
    public void Success_ShouldReturnSuccessResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.ValidationErrors.Should().BeNull();
        result.ErrorArgs.Should().BeEmpty();
    }

    [Fact]
    public void SuccessT_ShouldReturnSuccessResultWithValue()
    {
        var value = Create<int>();
        var result = Result.Success(value);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(value);
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.ValidationErrors.Should().BeNull();
    }

    [Fact]
    public void Fail_ShouldReturnFailureResult()
    {
        var errorCode = Create<string>();
        var errorMessage = Create<string>();
        var arg1 = Create<string>();
        var arg2 = Create<int>();

        var result = Result.Fail(errorCode, errorMessage, arg1, arg2);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(errorCode);
        result.ErrorMessage.Should().Be(errorMessage);
        result.ErrorArgs.Should().Contain(arg1);
        result.ErrorArgs.Should().Contain(arg2);
        result.ValidationErrors.Should().BeNull();
    }

    [Fact]
    public void FailT_ShouldReturnFailureResultWithValue()
    {
        var errorCode = Create<string>();
        var errorMessage = Create<string>();
        var baseResult = Result.Fail(errorCode, errorMessage);
        var result = Result.Fail<int>(baseResult);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(errorCode);
        result.ErrorMessage.Should().Be(errorMessage);
        result.Value.Should().Be(default);
    }

    [Fact]
    public void ValidationFailed_ShouldReturnValidationFailureResult()
    {
        var errors = new Dictionary<string, string[]> { { "Field", new[] { "Required" } } };
        var result = Result.ValidationFailed(errors);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("Validation.Failed");
        result.ErrorMessage.Should().Be("Validation failed.");
        result.ValidationErrors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void ValidationFailedT_ShouldReturnValidationFailureResultWithValue()
    {
        var errors = new Dictionary<string, string[]> { { "Field", new[] { "Required" } } };
        var result = Result.ValidationFailed<int>(errors);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("Validation.Failed");
        result.ErrorMessage.Should().Be("Validation failed.");
        result.ValidationErrors.Should().BeEquivalentTo(errors);
        result.Value.Should().Be(default);
    }

    [Fact]
    public void ToString_ShouldReturnSuccessOrFailureMessage()
    {
        var success = Result.Success();
        var failure = Result.Fail("ERR", "Error occurred");

        success.ToString().Should().Be("Success");
        failure.ToString().Should().Be("Failure: Error occurred");
    }

    [Fact]
    public void ErrorArgs_ShouldBeNull_WhenNoArgsProvided()
    {
        var result = Result.Fail("ERR", "Error occurred");

        result.ErrorArgs.Should().NotBeNull();
        result.ErrorArgs.Should().BeEmpty();
    }

    [Fact]
    public void ResultT_ShouldSupportReferenceTypes()
    {
        var value = Create<string>();
        var result = Result.Success(value);

        result.Value.Should().Be(value);
    }

    [Fact]
    public void ResultT_ShouldSupportComplexTypes()
    {
        var mock = new Mock<IComparable<int>>();
        var result = Result.Success(mock.Object);

        result.Value.Should().BeAssignableTo<IComparable<int>>();
    }
}

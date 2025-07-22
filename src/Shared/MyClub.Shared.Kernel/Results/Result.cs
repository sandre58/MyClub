// -----------------------------------------------------------------------
// <copyright file="Result.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;

namespace MyClub.Shared.Kernel.Results;

public class Result
{
    protected Result(
        bool isSuccess,
        string? errorCode = null,
        string? errorMessage = null,
        Dictionary<string, string[]>? validationErrors = null,
        params object?[] args)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors;
        ErrorArgs = args.ToList().AsReadOnly();
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public Dictionary<string, string[]>? ValidationErrors { get; }

    public IReadOnlyList<object?> ErrorArgs { get; }

    public static Result Success() => new(true);

    public static Result<T> Success<T>(T value) => new(true, value);

    public static Result Fail(string errorCode, string? message = null, params object?[] args)
        => new(false, errorCode, message, null, args);

    public static Result<T> Fail<T>(Result result) => Fail<T>(result.ErrorCode.OrEmpty(), result.ErrorMessage, result.ErrorArgs);

    public static Result<T> Fail<T>(string errorCode, string? message = null, params object?[] args)
        => new(false, default!, errorCode, message, null, args);

    public static Result ValidationFailed(Dictionary<string, string[]> errors)
        => new(false, "Validation.Failed", "Validation failed.", errors);

    public static Result<T> ValidationFailed<T>(Dictionary<string, string[]> errors)
        => new(false, default!, "Validation.Failed", "Validation failed.", errors);

    public override string ToString() => IsSuccess ? "Success" : $"Failure: {ErrorMessage}";
}

public class Result<T> : Result
{
    internal Result(
            bool isSuccess,
            T value,
            string? errorCode = null,
            string? errorMessage = null,
            Dictionary<string, string[]>? validationErrors = null,
            params object?[] args)
            : base(isSuccess, errorCode, errorMessage, validationErrors, args) => Value = value;

    public T Value { get; }
}

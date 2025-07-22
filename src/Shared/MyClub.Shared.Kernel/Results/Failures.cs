// -----------------------------------------------------------------------
// <copyright file="Failures.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace MyClub.Shared.Kernel.Results;

public static class Failures
{
    public static Result<T> AlreadyExists<T>(string identifier) => Result.Fail<T>(GetErrorCode(), $"'{identifier}' already exists.", identifier);

    public static Result<T> NameAlreadyExists<T>(string name) => Result.Fail<T>(GetErrorCode(), $"An item with name '{name}' already exists.", name);

    public static Result<T> NotFound<T>(string identifier) => Result.Fail<T>(GetErrorCode(), $"Item '{identifier}' not found.", identifier);

    public static Result NotFound(string identifier) => Result.Fail(GetErrorCode(), $"Item '{identifier}' not found.", identifier);

    public static Result<T> TeamIsAlreadyAssignedToFeature<T>(string identifier) => Result.Fail<T>(GetErrorCode(), $"'{identifier}' already assigned to a fixture in round.", identifier);

    private static string GetErrorCode([CallerMemberName] string errorMethodName = "") => $"Errors.{errorMethodName}";
}

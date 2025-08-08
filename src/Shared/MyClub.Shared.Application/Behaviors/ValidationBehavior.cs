// -----------------------------------------------------------------------
// <copyright file="ValidationBehavior.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically validates requests using FluentValidation.
/// This behavior intercepts all requests and validates them against registered validators before processing.
/// If validation fails, it either throws a ValidationException or returns a failed Result depending on the response type.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
/// <typeparam name="TResponse">The type of response being returned.</typeparam>
/// <param name="validators">Collection of validators to apply to the request.</param>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Handles the request validation in the MediatR pipeline.
    /// Validates the request using all registered validators and either continues processing or returns validation errors.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// The response from the next handler if validation succeeds, or a failed Result containing validation errors.
    /// For non-Result response types, throws a ValidationException on validation failure.
    /// </returns>
    /// <exception cref="ValidationException">
    /// Thrown when validation fails and the response type is not a Result type.
    /// </exception>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken).ConfigureAwait(false);

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count == 0)
            return await next(cancellationToken).ConfigureAwait(false);

        var resultType = typeof(TResponse);

        if (!resultType.IsGenericType || resultType.GetGenericTypeDefinition() != typeof(Result<>))
            throw new ValidationException(failures);

        var errorDict = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

        var errorObject = typeof(Result<>)
            .MakeGenericType(resultType.GenericTypeArguments[0])
            .GetMethod("FailValidation")!
            .Invoke(null, [errorDict]);

        return (TResponse)errorObject!;
    }
}

// -----------------------------------------------------------------------
// <copyright file="CorrelationIdMiddleware.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Serilog.Context;

namespace MyClub.PlayUp.Host;

/// <summary>
/// Accepts or generates <c>X-Correlation-Id</c>, enriches Serilog LogContext, and echoes the header on all responses.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">HTTP context.</param>
    /// <returns>A task that completes when the pipeline finishes.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var inbound = context.Request.Headers[CorrelationId.HeaderName].ToString();
        var correlationId = CorrelationId.IsValid(inbound) ? inbound : CorrelationId.NewId();
        context.Items[CorrelationId.ItemKey] = correlationId;

        context.Response.OnStarting(
            static state =>
            {
                var httpContext = (HttpContext)state;
                var id = httpContext.GetCorrelationId();
                httpContext.Response.Headers[CorrelationId.HeaderName] = id;
                return Task.CompletedTask;
            },
            context);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}

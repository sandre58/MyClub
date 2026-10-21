// -----------------------------------------------------------------------
// <copyright file="MediaEndpoints.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Media.Application.Media;
using MyClub.Media.Domain;
using MyClub.PlayUp.Host.Contracts;

namespace MyClub.PlayUp.Host.Endpoints;

internal static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/media",
            async (HttpRequest request, MediaService mediaService, CancellationToken cancellationToken) =>
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new { title = "Expected multipart/form-data with a 'file' field." });
                }

                var form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
                var file = form.Files.GetFile("file");
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest(new { title = "A non-empty 'file' form field is required." });
                }

                await using var stream = file.OpenReadStream();
                var metadata = await mediaService
                    .CreateAsync(stream, file.ContentType, file.Length, file.FileName, cancellationToken)
                    .ConfigureAwait(false);

                return Results.Created(
                    $"/media/{metadata.Id}",
                    new MediaMetadataResponse(
                        metadata.Id,
                        metadata.ContentType,
                        metadata.ByteSize,
                        metadata.OriginalName,
                        metadata.CreatedAt));
            });

        app.MapGet(
            "/media/{mediaId:guid}",
            async (Guid mediaId, MediaService mediaService, CancellationToken cancellationToken) =>
            {
                var metadata = await mediaService
                    .GetMetadataAsync(new MediaId(mediaId), cancellationToken)
                    .ConfigureAwait(false);

                return Results.Ok(
                    new MediaMetadataResponse(
                        metadata.Id,
                        metadata.ContentType,
                        metadata.ByteSize,
                        metadata.OriginalName,
                        metadata.CreatedAt));
            });

        app.MapGet(
            "/media/{mediaId:guid}/content",
            async (Guid mediaId, MediaService mediaService, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var content = await mediaService
                    .OpenContentAsync(new MediaId(mediaId), cancellationToken)
                    .ConfigureAwait(false);

                httpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";

                return Results.File(
                    content.Content,
                    content.ContentType,
                    enableRangeProcessing: false);
            });

        app.MapDelete(
            "/media/{mediaId:guid}",
            async (Guid mediaId, MediaService mediaService, CancellationToken cancellationToken) =>
            {
                await mediaService.DeleteAsync(new MediaId(mediaId), cancellationToken).ConfigureAwait(false);
                return Results.NoContent();
            });

        return app;
    }
}

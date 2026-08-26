// -----------------------------------------------------------------------
// <copyright file="DatasetDocument.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Datasets;

/// <summary>
/// JSON dataset shape for inspired competitions and team lists.
/// </summary>
public sealed class DatasetDocument
{
    /// <summary>Gets or sets the stable dataset key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the competition display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional competition short name.</summary>
    public string? ShortName { get; set; }

    /// <summary>Gets or sets the optional competition logo asset under <c>Assets/seed-logos/</c>.</summary>
    public string? LogoAsset { get; set; }

    /// <summary>Gets or sets the optional declared start (ISO).</summary>
    public DateTimeOffset? ScheduledStart { get; set; }

    /// <summary>Gets or sets the optional declared end (ISO).</summary>
    public DateTimeOffset? ScheduledEnd { get; set; }

#pragma warning disable CA1002, CA2227 // Bound from JSON.
    /// <summary>Gets or sets ordered team rows.</summary>
    public List<DatasetTeamDocument> Teams { get; set; } = [];
#pragma warning restore CA1002, CA2227
}

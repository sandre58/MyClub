// -----------------------------------------------------------------------
// <copyright file="DatasetTeamDocument.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Datasets;

/// <summary>
/// One team row in a Development dataset JSON.
/// </summary>
public sealed class DatasetTeamDocument
{
    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional short name.</summary>
    public string? ShortName { get; set; }

    /// <summary>Gets or sets the optional logo asset under <c>Assets/seed-logos/</c>.</summary>
    public string? LogoAsset { get; set; }

    /// <summary>Gets or sets the optional primary kit color (#RRGGBB).</summary>
    public string? PrimaryColor { get; set; }

    /// <summary>Gets or sets the optional secondary kit color (#RRGGBB).</summary>
    public string? SecondaryColor { get; set; }
}

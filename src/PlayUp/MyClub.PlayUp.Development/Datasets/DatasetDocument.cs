// -----------------------------------------------------------------------
// <copyright file="DatasetDocument.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Datasets;

/// <summary>
/// JSON dataset shape for inspired team lists.
/// </summary>
public sealed class DatasetDocument
{
    /// <summary>Gets or sets the stable dataset key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the competition display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

#pragma warning disable CA1002, CA2227 // Bound from JSON.
    /// <summary>Gets or sets ordered team display names.</summary>
    public List<string> Teams { get; set; } = [];
#pragma warning restore CA1002, CA2227
}

// -----------------------------------------------------------------------
// <copyright file="IAuditable.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Kernel.Interfaces;

/// <summary>
/// Interface for objects that support audit tracking.
/// Provides properties and methods to track when an object was created or modified and by whom.
/// This interface is typically implemented by entities that need to maintain an audit trail.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets the timestamp when this object was created.
    /// </summary>
    DateTime? CreatedAt { get; }

    /// <summary>
    /// Gets the identifier of the user who created this object.
    /// </summary>
    string? CreatedBy { get; }

    /// <summary>
    /// Gets the timestamp when this object was last modified.
    /// </summary>
    DateTime? ModifiedAt { get; }

    /// <summary>
    /// Gets the identifier of the user who last modified this object.
    /// </summary>
    string? ModifiedBy { get; }

    /// <summary>
    /// Marks the object as modified with the specified timestamp and user.
    /// This method is typically called by the infrastructure layer when saving changes.
    /// </summary>
    /// <param name="modifiedAt">The timestamp when the object was modified.</param>
    /// <param name="modifiedBy">The identifier of the user who modified the object. Can be null if not available.</param>
    void MarkedAsModified(DateTime? modifiedAt, string? modifiedBy = null);

    /// <summary>
    /// Marks the object as created with the specified timestamp and user.
    /// This method should clear any previous modification information and set the creation audit data.
    /// This method is typically called by the infrastructure layer when creating a new object.
    /// </summary>
    /// <param name="createdAt">The timestamp when the object was created.</param>
    /// <param name="createdBy">The identifier of the user who created the object. Can be null if not available.</param>
    void MarkedAsCreated(DateTime? createdAt, string? createdBy = null);
}

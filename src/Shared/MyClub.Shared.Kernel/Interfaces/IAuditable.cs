// -----------------------------------------------------------------------
// <copyright file="IAuditable.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Kernel.Interfaces;

public interface IAuditable
{
    DateTime? CreatedAt { get; }

    string? CreatedBy { get; }

    DateTime? ModifiedAt { get; }

    string? ModifiedBy { get; }

    void MarkedAsModified(DateTime? modifiedAt, string? modifiedBy = null);

    void MarkedAsCreated(DateTime? createdAt, string? createdBy = null);
}

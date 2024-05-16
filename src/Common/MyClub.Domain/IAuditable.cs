// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Domain
{
    public interface IAuditable
    {
        DateTime? CreatedAt { get; }

        string? CreatedBy { get; }

        DateTime? ModifiedAt { get; }

        string? ModifiedBy { get; }

        void MarkedAsModified(DateTime? modifiedAt, string? modifiedBy = null);

        void MarkedAsCreated(DateTime? createdAt, string? createdBy = null);
    }
}

// -----------------------------------------------------------------------
// <copyright file="IAuditService.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.CrossCutting.Auditing;

public interface IAuditService
{
    string GetCurrentUser();

    DateTime GetCurrentTimestamp();
}

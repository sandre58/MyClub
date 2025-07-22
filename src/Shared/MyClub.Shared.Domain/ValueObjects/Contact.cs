// -----------------------------------------------------------------------
// <copyright file="Contact.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.ValueObjects;

public record Contact(string Value, string? Label = null, bool IsDefault = false);

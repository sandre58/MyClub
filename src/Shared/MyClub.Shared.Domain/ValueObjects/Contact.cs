// -----------------------------------------------------------------------
// <copyright file="Contact.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.ValueObjects;

/// <summary>
/// Base record representing contact information with a value, optional label, and default flag.
/// This serves as a foundation for specific contact types like email addresses and phone numbers.
/// </summary>
/// <param name="Value">The contact value (email address, phone number, etc.).</param>
/// <param name="Label">An optional descriptive label for this contact (e.g., "Work", "Home", "Personal").</param>
/// <param name="IsDefault">Indicates whether this is the default/primary contact of its type.</param>
public record Contact(string Value, string? Label = null, bool IsDefault = false);

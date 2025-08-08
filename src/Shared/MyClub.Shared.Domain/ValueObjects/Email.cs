// -----------------------------------------------------------------------
// <copyright file="Email.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyNet.Utilities;

namespace MyClub.Shared.Domain.ValueObjects;

/// <summary>
/// Value object representing an email address contact.
/// Extends the base Contact with email address validation to ensure the value is a valid email format.
/// The email address is validated both for required presence and correct email format.
/// </summary>
/// <param name="Value">The email address value. Must be a valid email address format.</param>
/// <param name="Label">An optional descriptive label for this email (e.g., "Work", "Personal").</param>
/// <param name="IsDefault">Indicates whether this is the default/primary email address.</param>
public record Email(string Value, string? Label = null, bool IsDefault = false) : Contact(Value.IsRequiredOrThrow(nameof(Email)).IsEmailAddressOrThrow(nameof(Email)), Label, IsDefault);

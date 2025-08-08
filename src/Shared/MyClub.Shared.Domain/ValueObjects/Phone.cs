// -----------------------------------------------------------------------
// <copyright file="Phone.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyNet.Utilities;

namespace MyClub.Shared.Domain.ValueObjects;

/// <summary>
/// Value object representing a phone number contact.
/// Extends the base Contact with phone number validation to ensure the value is a valid phone number format.
/// The phone number is validated both for required presence and correct phone number format.
/// </summary>
/// <param name="Value">The phone number value. Must be a valid phone number format.</param>
/// <param name="Label">An optional descriptive label for this phone number (e.g., "Mobile", "Home", "Work").</param>
/// <param name="IsDefault">Indicates whether this is the default/primary phone number.</param>
public record Phone(string Value, string? Label = null, bool IsDefault = false) : Contact(Value.IsRequiredOrThrow(nameof(Phone)).IsPhoneNumberOrThrow(nameof(Phone)), Label, IsDefault);

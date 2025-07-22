// -----------------------------------------------------------------------
// <copyright file="Phone.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyNet.Utilities;

namespace MyClub.Shared.Domain.ValueObjects;

public record Phone(string Value, string? Label = null, bool IsDefault = false) : Contact(Value.IsRequiredOrThrow(nameof(Phone)).IsPhoneNumberOrThrow(nameof(Phone)), Label, IsDefault);

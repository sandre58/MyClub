// -----------------------------------------------------------------------
// <copyright file="Email.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyNet.Utilities;

namespace MyClub.Shared.Domain.ValueObjects;

public record Email(string Value, string? Label = null, bool IsDefault = false) : Contact(Value.IsRequiredOrThrow(nameof(Email)).IsEmailAddressOrThrow(nameof(Email)), Label, IsDefault);

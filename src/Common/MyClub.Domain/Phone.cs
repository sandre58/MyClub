﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Utilities.Extensions;

namespace MyClub.Domain
{
    public class Phone(string value, string? label = null, bool isDefault = false) : Contact(value, label, isDefault)
    {
        protected override string ValidateOrThrowValue(string value) => value.IsRequiredOrThrow(nameof(Phone)).IsPhoneNumberOrThrow(nameof(Email));
    }
}

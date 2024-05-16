// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;
using MyNet.Observable;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableStadiumViewModel(Guid? id = null) : EditableObject
    {
        public Guid? Id { get; } = id;

        public Address? Address { get; set; }

        public string? Name { get; set; }

        [DependsOn(nameof(Name), nameof(Address))]
        public string DisplayName => string.Join(", ", new[] { string.IsNullOrWhiteSpace(Address?.City) ? null : Address?.City, Name }.NotNull());

        public Ground Ground { get; set; }

        public override string ToString() => DisplayName;
    }

}

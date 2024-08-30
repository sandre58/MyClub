// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableStadiumViewModel : EditableObject, IStadiumViewModel
    {
        public Guid Id { get; } = Guid.NewGuid();

        public Address? Address { get; set; }

        public string Name { get; set; } = string.Empty;

        [DependsOn(nameof(Name), nameof(Address))]
        public string DisplayName => string.Join(", ", new[] { Name, string.IsNullOrWhiteSpace(Address?.City) ? null : Address?.City }.NotNull());

        public Ground Ground { get; set; }

        public override string ToString() => DisplayName;
    }

}

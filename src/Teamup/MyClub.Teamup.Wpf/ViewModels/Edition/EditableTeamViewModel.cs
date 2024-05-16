// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Media;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyNet.Observable;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableTeamViewModel(Guid? id = null) : EditableObject, ISimilar<EditableTeamViewModel>, IOrderable
    {
        public Guid? Id { get; } = id;

        public Guid TemporaryId { get; } = Guid.NewGuid();

        public string ClubName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string ShortName { get; set; } = string.Empty;

        public Category? Category { get; set; }

        public byte[]? Logo { get; set; }

        public Color? HomeColor { get; set; }

        public Color? AwayColor { get; set; }

        public Country? Country { get; set; }

        public EditableStadiumViewModel? Stadium { get; set; }

        public int Order { get; set; }

        public bool IsMyTeam { get; set; }

        public bool IsSimilar(EditableTeamViewModel? obj) => Name.ToLower().Equals(obj?.Name.ToLower()) && Equals(Category, obj?.Category);

        public override string ToString() => Name;
    }
}

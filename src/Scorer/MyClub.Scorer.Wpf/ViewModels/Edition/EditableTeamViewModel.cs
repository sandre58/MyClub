// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Media;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableTeamViewModel : EditableObject, ISimilar<EditableTeamViewModel>, ITeamViewModel
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string ShortName { get; set; } = string.Empty;

        public byte[]? Logo { get; set; }

        public Color? HomeColor { get; set; }

        public Color? AwayColor { get; set; }

        public Country? Country { get; set; }

        public EditableStadiumViewModel? Stadium { get; set; }

        IStadiumViewModel? ITeamViewModel.Stadium => Stadium;

        public bool IsSimilar(EditableTeamViewModel? obj) => Name.ToLower().Equals(obj?.Name.ToLower());

        public override string ToString() => Name;
    }
}

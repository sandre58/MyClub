// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using MyNet.Observable.Attributes;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Application.Dtos;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class CompetitionImportableViewModel : ImportableViewModel
    {
        public CompetitionImportableViewModel(CompetitionType type, string name, CompetitionRules rules, ImportMode mode = ImportMode.Add, bool import = false)
            : base(mode, import)
        {
            Name = name;
            Type = type;
            CompetitionRules = rules;
        }

        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string ShortName { get; set; } = string.Empty;

        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Name { get; }

        public byte[]? Logo { get; set; }

        public Category? Category { get; set; }

        public CompetitionType? Type { get; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public CompetitionRules CompetitionRules { get; }
    }
}

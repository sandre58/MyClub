﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.Enums;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class ProjectCreationGeneralViewModel : NavigableWorkspaceViewModel
    {
        [IsRequired]
        [Display(Name = nameof(Type), ResourceType = typeof(MyClubResources))]
        public CompetitionType Type { get; set; }

        public bool TreatNoStadiumAsWarning { get; set; }

        [IsRequired]
        [Display(Name = "PeriodForPreviousMatches", ResourceType = typeof(MyClubResources))]
        public int? PeriodForPreviousMatchesValue { get; set; }

        [IsRequired]
        [Display(Name = "PeriodForPreviousMatches", ResourceType = typeof(MyClubResources))]
        public TimeUnit PeriodForPreviousMatchesUnit { get; set; }

        [IsRequired]
        [Display(Name = "PeriodForNextMatches", ResourceType = typeof(MyClubResources))]
        public int? PeriodForNextMatchesValue { get; set; }

        [IsRequired]
        [Display(Name = "PeriodForNextMatches", ResourceType = typeof(MyClubResources))]
        public TimeUnit PeriodForNextMatchesUnit { get; set; }

        protected override void ResetCore()
        {
            Type = CompetitionType.League;
            TreatNoStadiumAsWarning = true;
            PeriodForNextMatchesValue = 8;
            PeriodForNextMatchesUnit = TimeUnit.Day;
            PeriodForPreviousMatchesUnit = TimeUnit.Day;
            PeriodForPreviousMatchesValue = 8;
        }
    }
}

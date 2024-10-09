﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Scorer.Wpf.Controls
{
    internal class CompetitionStageButton : ItemButton<IStageViewModel>
    {
        static CompetitionStageButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(CompetitionStageButton), new FrameworkPropertyMetadata(typeof(CompetitionStageButton)));

        public CompetitionStageButton() { }
    }
}
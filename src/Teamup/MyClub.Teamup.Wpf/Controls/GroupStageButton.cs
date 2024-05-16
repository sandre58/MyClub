﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Windows;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Controls
{
    internal class GroupStageButton : ItemButton<GroupStageViewModel>
    {
        static GroupStageButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(GroupStageButton), new FrameworkPropertyMetadata(typeof(GroupStageButton)));

        public GroupStageButton() { }
    }
}

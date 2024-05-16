// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.UI.Dialogs.Models;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal interface IEntityEditionViewModel : IDialogViewModel
    {
        void New(Action? initialize = null);

        void Load(Guid id);

        Guid? ItemId { get; }
    }
}

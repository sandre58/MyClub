// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.Dialogs;
using MyNet.UI.Dialogs.Settings;
using MyNet.Wpf.Dialogs;

namespace MyClub.Scorer.Wpf.ViewModels.MessageBox
{
    internal class RemoveTeamsMessageBoxViewModel : MessageBoxViewModel
    {
        public RemoveTeamsMessageBoxViewModel(string message, string? title = null, MessageSeverity severity = MessageSeverity.Information, MessageBoxResultOption buttons = MessageBoxResultOption.OkCancel, MessageBoxResult defaultResult = MessageBoxResult.OK)
            : base(message, title, severity, buttons, defaultResult)
        {
        }

        public bool RemoveStadium { get; set; } = true;
    }
}

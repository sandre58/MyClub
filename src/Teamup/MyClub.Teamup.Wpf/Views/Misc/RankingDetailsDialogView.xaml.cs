// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Wpf.Dialogs;

namespace MyClub.Teamup.Wpf.Views.Misc
{
    public partial class RankingDetailsDialogView : IOverlayDialog
    {
        public RankingDetailsDialogView() => InitializeComponent();

        public bool CloseOnClickAway => true;

        public bool FocusOnShow => true;
    }
}

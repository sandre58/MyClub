// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.Dialogs;
using MyNet.Utilities.Google.Maps;

namespace MyClub.Teamup.Wpf.ViewModels.Misc
{
    internal class GoogleMapDialogViewModel : DialogViewModel
    {
        public string Url { get; }

        public GoogleMapDialogViewModel(string address) => Url = GoogleMapsHelper.GetGoogleMapsUrl(new GoogleMapsSettings { Address = address, HideLeftPanel = true });
    }
}

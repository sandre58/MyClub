// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.Encryption;
using MyNet.Utilities.Mail;
using MyNet.Observable.Attributes;
using MyClub.CrossCutting.Localization;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class SettingsEditionViewModel : EditionViewModel
    {
        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool MustRestart { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool SavedAndMustRestart { get; private set; }

        public SmtpSettingsEditionViewModel SmtpSettingsEditionViewModel { get; set; }

        public SettingsEditionViewModel(
            IMailServiceFactory mailServiceFactory,
            IEncryptionService encryptionService)
        {
            Mode = ScreenMode.Edition;

            SmtpSettingsEditionViewModel = new SmtpSettingsEditionViewModel(mailServiceFactory, encryptionService);

            AddSubWorkspaces(
            [
                SmtpSettingsEditionViewModel
            ]);

            Disposables.Add(SubWorkspaces.ToObservableChangeSet().OnItemAdded(x =>
                Disposables.Add(Observable.FromEventPattern<PropertyChangedEventHandler?, PropertyChangedEventArgs>(handler => x.PropertyChanged += handler, handler => x.PropertyChanged -= handler).Where(x => ((SettingsEditionTabViewModel)x.Sender!).GetPropertiesAppliedAfterRestart().Contains(x.EventArgs.PropertyName)).Subscribe(_ =>
                {
                    if (!IsModifiedSuspender.IsSuspended)
                        MustRestart = true;
                }))).Subscribe());
        }
        protected override string CreateTitle() => MyClubResources.Settings;

        protected override void RefreshCore()
        {
            using (IsModifiedSuspender.Suspend())
            {
                base.RefreshCore();
                MustRestart = false;
            }
        }
        protected override void SaveCore()
        {
            AllWorkspaces.OfType<SettingsEditionTabViewModel>().ForEach(x => x.Save());

            if (MustRestart)
            {
                ToasterManager.ShowWarning(MyClubResources.RestartApplicationWarning, ToastClosingStrategy.CloseButton);
                SavedAndMustRestart = true;
            }
        }
    }
}

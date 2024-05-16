// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Dialogs.Settings;
using MyNet.Wpf.DragAndDrop;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Observable.Collections;
using MyNet.Observable.Attributes;
using MyClub.CrossCutting.Localization;

namespace MyClub.Teamup.Wpf.ViewModels.CommunicationPage
{
    internal class EmailViewModel : WorkspaceViewModel
    {
        [IsRequired]
        [Display(Name = nameof(Subject), ResourceType = typeof(MyClubResources))]
        public string Subject { get; set; } = string.Empty;

        [IsRequired]
        [Display(Name = nameof(Body), ResourceType = typeof(MyClubResources))]
        public string Body { get; set; } = string.Empty;

        [Display(Name = nameof(SendACopy), ResourceType = typeof(MyClubResources))]
        public bool SendACopy { get; set; }

        public ICommand AddAttachmentCommand { get; private set; }

        public ICommand RemoveAttachmentCommand { get; private set; }

        public FileDropHandler DropHandler { get; }

        public ObservableCollection<string> Attachments { get; } = new ThreadSafeObservableCollection<string>();

        public EmailViewModel()
        {
            AddAttachmentCommand = CommandsManager.Create(async () => await AddAttachmentAsync().ConfigureAwait(false));
            RemoveAttachmentCommand = CommandsManager.Create<string>(RemoveAttachment);

            DropHandler = new((info, files) => AddAttachments(files.OfType<string>()));
        }

        protected override void ResetCore()
        {
            SendACopy = false;
            Subject = string.Empty;
            Body = string.Empty;
            Attachments.Clear();
        }

        private async Task AddAttachmentAsync()
        {
            var settings = new OpenFileDialogSettings { Multiselect = true };
            var result = await DialogManager.ShowOpenFileDialogAsync(settings).ConfigureAwait(false);
            if (result.result.IsTrue())
                AddAttachments(settings.FileNames);
        }

        private void AddAttachments(IEnumerable<string> filenames)
            => filenames.Where(x => !string.IsNullOrEmpty(x) && !Attachments.Contains(x)).ToList().ForEach(Attachments.Add);

        private void RemoveAttachment(string? path)
        {
            if (path is not null && Attachments.Contains(path))
                _ = Attachments.Remove(path);
        }
    }
}

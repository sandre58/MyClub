// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Threading;
using MyNet.UI.Commands;
using MyNet.UI.Selection;
using MyNet.UI.Selection.Models;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Wpf.Schedulers;

namespace MyClub.Teamup.Wpf.ViewModels.CommunicationPage
{
    internal class PlayersViewModel : SelectionListViewModel<PlayerViewModel>
    {
        private readonly ReadOnlyObservableCollection<Email> _selectedEmails;

        public ReadOnlyObservableCollection<Email> SelectedEmails => _selectedEmails;

        public PlayersViewModel(PlayersProvider playersProvider)
            : base(new SelectableCollection<PlayerViewModel>(playersProvider.Connect(), createWrapper: x => new PlayerEmailsWrapper(x), scheduler: Scheduler.UI),
                  parametersProvider: new PlayersListParametersProvider())
        {
            PresetSelections.Add(MyClubResources.SelectOnlyDefaultEmails, CommandsManager.Create(() => Wrappers.ForEach(x => ((PlayerEmailsWrapper)x).SelectDefaultEmails()), () => Wrappers.Any(x => CanChangeSelectedState(x, true))));

            Disposables.Add(Wrappers.ToObservableChangeSet(x => x.Id)
                .MergeManyEx(x => ((PlayerEmailsWrapper)x).Emails.ToObservableChangeSet(x => x.Id), x => x.Id)
                .AutoRefresh(x => x.IsSelected)
                .Filter(x => x.IsSelected)
                .Transform(x => x.Item)
                .Bind(out _selectedEmails)
                .Subscribe());

        }

        protected override void ResetCore() => UnselectAll();

        protected override void SelectAll() => Wrappers.ForEach(x => ((PlayerEmailsWrapper)x).ToggleAllEmails(true));

        protected override void UnselectAll() => Wrappers.ForEach(x => ((PlayerEmailsWrapper)x).ToggleAllEmails(false));

        protected override void ClearSelection() => WrappersSource.ForEach(x => ((PlayerEmailsWrapper)x).ToggleAllEmails(false));

        public void SelectAddresses(IEnumerable<string> addresses)
        {
            foreach (var item in Wrappers)
            {
                foreach (var email in (item as PlayerEmailsWrapper)!.Emails)
                    email.IsSelected = addresses.Contains(email.Item!.Value);
            }
        }
    }

    internal sealed class PlayerEmailsWrapper : SelectedWrapper<PlayerViewModel>
    {
        public override bool IsSelected => _emails.Any(x => x.IsSelected);

        private readonly ReadOnlyObservableCollection<EmailWrapper> _emails;
        private readonly ReadOnlyObservableCollection<Email> _selectedEmails;

        public ReadOnlyObservableCollection<EmailWrapper> Emails => _emails;

        public ReadOnlyObservableCollection<Email> SelectedEmails => _selectedEmails;

        public ICommand ToggleAllEmailsCommand { get; }

        public PlayerEmailsWrapper(PlayerViewModel item) : base(item)
        {
            ToggleAllEmailsCommand = CommandsManager.CreateNotNull<bool>(ToggleAllEmails, x => _emails?.Any(y => y.IsSelectable) ?? false);

            Disposables.AddRange(
            [
                item.Emails.ToObservableChangeSet()
                           .Transform(x => new EmailWrapper(x))
                           .AutoRefresh(x => x.Item.Default)
                           .Sort(SortExpressionComparer<EmailWrapper>.Descending(x => x.Item.Default))
                           .ObserveOn(WpfScheduler.Current)
                           .Bind(out _emails)
                           .Subscribe(),
                _emails.ToObservableChangeSet().Subscribe(_ => IsSelectable = _emails.Any()),
                _emails.ToObservableChangeSet(x => x.Id).AutoRefresh(x => x.IsSelected).Filter(x => x.IsSelected).Transform(x => x.Item).Bind(out _selectedEmails).Subscribe(),
                _selectedEmails.ToObservableChangeSet().Subscribe(x => RaisePropertyChanged(nameof(IsSelected)))
            ]);
        }

        public void ToggleAllEmails(bool value) => _emails.ForEach(x => x.IsSelected = value);

        public void SelectDefaultEmails() => _emails.ForEach(x => x.IsSelected = x.Item.Default);

    }

    internal class EmailWrapper : SelectedWrapper<Email>
    {
        public EmailWrapper(Email item) : base(item) { }
    }
}

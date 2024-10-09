// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MaterialDesignThemes.Wpf;
using MyNet.Utilities;
using MyNet.Observable.Attributes;
using MyNet.Observable.Translatables;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Utilities.Extensions;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class ActionViewModel : TranslatableString, ISearchableItem
    {
        private readonly Action _action;
        private readonly string _descriptionKey;

        public ActionViewModel(string key, string descriptionKey, Action action, PackIconKind icon) : base(key)
        {
            _action = action;
            Icon = icon;
            _descriptionKey = descriptionKey;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public PackIconKind Icon { get; set; }

        [UpdateOnCultureChanged]
        public string? Description => _descriptionKey.Translate();

        public void Open() => _action();

        #region ISearchableItem

        public string SearchDisplayName => Value.OrEmpty();

        public string SearchText => Value.OrEmpty();

        public string SearchCategory => nameof(MyClubResources.Actions);

        #endregion
    }
}

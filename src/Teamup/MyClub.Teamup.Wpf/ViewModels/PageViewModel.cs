// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities.Messaging;
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Messages;
using MyClub.Teamup.Wpf.Services.Providers;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels
{
    internal class PageViewModel : NavigableWorkspaceViewModel
    {
        private readonly ProjectInfoProvider _projectInfoProvider;

        public virtual bool CanDropTmprojFiles { get; }

        public bool HasCurrentProject { get; private set; }

        public Project? CurrentProject { get; private set; }

        protected PageViewModel(ProjectInfoProvider projectInfoProvider)
        {
            _projectInfoProvider = projectInfoProvider;

            projectInfoProvider.WhenProjectChanged(OnProjectChanged);

            Messenger.Default.Register<MainTeamChangedMessage>(this, OnMainTeamChanged);
        }

        [SuppressPropertyChangedWarnings]
        private void OnProjectChanged(Project? project)
        {
            CurrentProject = project;
            HasCurrentProject = project is not null;

            ResetFromProject(project);
        }

        protected virtual void ResetFromProject(Project? project) => ResetFromMainTeams(project?.GetMainTeams().Select(x => x.Id).ToList());

        [SuppressPropertyChangedWarnings]
        private void OnMainTeamChanged(MainTeamChangedMessage message) => ResetFromMainTeams(message.MainTeams.Select(x => x.Id).ToList());

        protected virtual void ResetFromMainTeams(IEnumerable<Guid>? mainTeams) { }

        protected override void RefreshCore()
        {
            if (!IsLoaded && CurrentProject is null)
                OnProjectChanged(_projectInfoProvider.GetCurrentProject());

            base.RefreshCore();
        }
    }
}

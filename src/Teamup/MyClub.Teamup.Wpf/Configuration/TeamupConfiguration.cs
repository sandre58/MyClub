// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace MyClub.Teamup.Wpf.Configuration
{
    internal class TeamupConfiguration
    {
        internal const string GoogleApiKey = "AIzaSyDBXlTBVMytLRMbEBg0k_nNwsN0nO41yAI";

        public class AuthenticationConfiguration
        {
            public string Registry { get; set; } = string.Empty;
        }

        public class RecentFilesConfiguration
        {
            public string Registry { get; set; } = string.Empty;

            public int Max { get; set; }
        }

        public class MockConfiguration
        {
            public string Directory { get; set; } = string.Empty;

            public string FactoryName { get; set; } = string.Empty;

            public bool RandomizeData { get; set; }
        }

        public class PluginsConfiguration
        {
            public string Directory { get; set; } = string.Empty;
        }
        public TeamupConfiguration() { }

        public TeamupConfiguration(IOptions<TeamupConfiguration> configuration)
        {
            DisableMail = configuration.Value.DisableMail;
            TempDirectory = configuration.Value.TempDirectory;
            Plugins.Directory = configuration.Value.Plugins.Directory;
            Mock.Directory = configuration.Value.Mock.Directory;
            Mock.RandomizeData = configuration.Value.Mock.RandomizeData;
            Mock.FactoryName = configuration.Value.Mock.FactoryName;
            RecentFiles.Registry = configuration.Value.RecentFiles.Registry;
            RecentFiles.Max = configuration.Value.RecentFiles.Max;
            Authentication.Registry = configuration.Value.Authentication.Registry;
        }

        public bool DisableMail { get; set; }

        public AuthenticationConfiguration Authentication { get; } = new();

        public RecentFilesConfiguration RecentFiles { get; } = new();

        public MockConfiguration Mock { get; } = new();

        public PluginsConfiguration Plugins { get; } = new();

        public string TempDirectory { get; set; } = string.Empty;
    }
}

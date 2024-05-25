// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace MyClub.Scorer.Wpf.Configuration
{
    internal class ScorerConfiguration
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
            public string FactoryPluginName { get; set; } = string.Empty;

            public bool RandomizeData { get; set; }
        }

        public class PluginsConfiguration
        {
            public string Directory { get; set; } = string.Empty;
        }

        public ScorerConfiguration() { }

        public ScorerConfiguration(IOptions<ScorerConfiguration> configuration)
        {
            DisableMail = configuration.Value.DisableMail;
            TempDirectory = configuration.Value.TempDirectory;
            RecentFiles.Registry = configuration.Value.RecentFiles.Registry;
            RecentFiles.Max = configuration.Value.RecentFiles.Max;
            Authentication.Registry = configuration.Value.Authentication.Registry;
            Plugins.Directory = configuration.Value.Plugins.Directory;
            Mock.RandomizeData = configuration.Value.Mock.RandomizeData;
            Mock.FactoryPluginName = configuration.Value.Mock.FactoryPluginName;
        }

        public bool DisableMail { get; set; }

        public AuthenticationConfiguration Authentication { get; } = new();

        public RecentFilesConfiguration RecentFiles { get; } = new();

        public MockConfiguration Mock { get; } = new();

        public PluginsConfiguration Plugins { get; } = new();

        public string TempDirectory { get; set; } = string.Empty;
    }
}

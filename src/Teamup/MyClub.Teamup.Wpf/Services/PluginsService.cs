// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using MyClub.Teamup.Plugins.Contracts.Base;
using MyNet.Utilities.Caching;

namespace MyClub.Teamup.Wpf.Services
{
    internal class PluginsService(string root, IServiceProvider serviceProvider)
    {
        private readonly CacheStorage<Assembly, List<Type>> _cache = new();

        private readonly string _root = root;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public bool HasPlugin<T>() where T : IPlugin
        {
            if (!_cache.Keys.Any())
                FindPlugins();

            return _cache.Keys.SelectMany(x => _cache[x]?.Where(x => x.IsAssignableTo(typeof(T))) ?? []).Any();
        }

        public IEnumerable<T> GetPlugins<T>(string? assemblyName = null) where T : IPlugin
        {
            if (!_cache.Keys.Any())
                FindPlugins();

            foreach (var assembly in _cache.Keys)
            {
                foreach (var type in _cache[assembly]?.Where(x => (string.IsNullOrEmpty(assemblyName) || x.Assembly.GetName().Name == assemblyName) && x.IsAssignableTo(typeof(T))) ?? [])
                {
                    var instance = (T?)ActivatorUtilities.CreateInstance(_serviceProvider, type);

                    if (instance is not null)
                        yield return instance;
                }
            }
        }

        public T? GetPlugin<T>(string? assemblyName = null) where T : IPlugin
        {
            var types = GetPlugins<T>(assemblyName);

            return types.FirstOrDefault();
        }

        private void FindPlugins()
        {
            if (!Directory.Exists(_root)) return;

            foreach (var item in new DirectoryInfo(_root).GetDirectories())
            {
                var dllPath = Path.Combine(item.FullName, $"{item.Name}.dll");

                if (File.Exists(dllPath))
                {
                    var assembly = LoadAssemblyFromDll(dllPath);

                    if (assembly is not null)
                        _cache.Add(assembly, assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(IPlugin))).ToList());
                }
            }
        }

        private static Assembly? LoadAssemblyFromDll(string dllPath, string? assemblyName = null)
        {
            if (!File.Exists(dllPath)) return null;

            var finalAssemblyName = assemblyName ?? Path.GetFileNameWithoutExtension(dllPath);

            var loadContext = new PluginLoadContext(dllPath);
            return loadContext.LoadFromAssemblyName(new AssemblyName(finalAssemblyName));
        }
    }

    internal class PluginLoadContext(string pluginPath) : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }

        protected override nint LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : nint.Zero;
        }
    }
}

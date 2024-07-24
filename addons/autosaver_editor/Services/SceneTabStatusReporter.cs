using System;
using System.Collections.Generic;
using AutoSaverPlugin.Contracts;
using AutoSaverPlugin.Shared;
using Godot;

namespace AutoSaverPlugin.Services
{
    internal sealed class SceneTabStatusReporter : ISceneStatusReporter
    {
        private readonly EditorInterface _editorInterface = EditorInterface.Singleton;
        private readonly ILoggerService _logger;

        public SceneTabStatusReporter(ILoggerService loggerService)
        {
            _logger = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        public List<string> FetchModifiedItems()
        {
            _logger.LogDebug("Fetching modified scenes...");
            var tabTitles = new List<string>();
            var tabBar = FindTabBar(_editorInterface.GetBaseControl());

            if (tabBar == null)
            {
                _logger.LogError("Scene tab bar not found.");
                return tabTitles;
            }

            for (int i = 0; i < tabBar.TabCount; i++)
            {
                var title = tabBar.GetTabTitle(i);
                _logger.LogDiagnostic($"Scene tab[{i}]: {title}");
                tabTitles.Add(title);
            }

            return tabTitles;
        }

        private TabBar FindTabBar(Node root)
        {
            if (root is TabBar tabBar)
            {
                return tabBar;
            }

            foreach (Node child in root.GetChildren())
            {
                var result = FindTabBar(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}

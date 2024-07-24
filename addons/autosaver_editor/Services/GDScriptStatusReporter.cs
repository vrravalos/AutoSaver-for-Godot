using System;
using System.Collections.Generic;
using System.Linq;
using AutoSaverPlugin.Contracts;
using AutoSaverPlugin.Shared;
using Godot;

namespace AutoSaverPlugin.Services
{
    internal sealed class GDScriptStatusReporter : IGDScriptStatusReporter
    {
        private readonly ILoggerService _logger;

        public GDScriptStatusReporter(ILoggerService loggerService)
        {
            _logger = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        public List<string> FetchModifiedItems()
        {
            _logger.LogDebug("Fetching modified scripts...");
            ScriptEditor editor = EditorInterface.Singleton.GetScriptEditor();

            List<string> listItemText = new();

            ItemList itemList = FindItemList(editor);

            if (itemList == null)
            {
                return listItemText;
            }

            for (int i = 0; i < itemList.ItemCount; i++)
            {
                var item = itemList.GetItemText(i);
                _logger.LogDiagnostic($"Script file[{i}]: {item}");
                listItemText.Add(item);
            }

            return listItemText;
        }

        private ItemList FindItemList(Node root)
        {
            if (root is ItemList itemList)
            {
                return itemList;
            }

            foreach (Node child in root.GetChildren())
            {
                var result = FindItemList(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}

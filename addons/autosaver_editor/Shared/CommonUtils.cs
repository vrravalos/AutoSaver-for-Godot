using System;
using System.Collections.Generic;
using Godot;

namespace AutoSaverPlugin.Shared
{
    internal static class CommonUtils
    {
        internal static string GetCurrentTimestamp() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        internal static string GetPluginVersion() => $"{PluginInfo.PluginVersion}";

        internal static List<string> GetAllProjectFiles(string extFileFilter = null)
        {
            List<string> files = new List<string>();
            EditorInterface editorInterface = EditorInterface.Singleton;
            EditorFileSystem fileSystem = editorInterface.GetResourceFilesystem();

            fileSystem.Scan();
            GetFilesRecursive(fileSystem.GetFilesystem(), files, extFileFilter);

            return files;
        }

        private static void GetFilesRecursive(EditorFileSystemDirectory directory, List<string> files, string extensionFilter = null)
        {
            for (int i = 0; i < directory.GetFileCount(); i++)
            {
                string filePath = directory.GetFilePath(i);
                if (string.IsNullOrEmpty(extensionFilter) || filePath.EndsWith(extensionFilter))
                {
                    files.Add(filePath);
                }
            }

            for (int i = 0; i < directory.GetSubdirCount(); i++)
            {
                GetFilesRecursive(directory.GetSubdir(i), files, extensionFilter);
            }
        }
    }
}

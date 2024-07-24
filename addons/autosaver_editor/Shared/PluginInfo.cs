using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSaverPlugin.Shared
{
    internal static class PluginInfo
    {
        internal const string NameShort = "AutoSaver";
        internal const string FullName = "AutoSaver Toggle for Godot Editor (C#)";
        internal const string Description = "Auto Saver for Godot Editor: a peace of mind toggle to automatically save your workspace";
        internal const string Author = "Victor R. R. Avalos";
        internal const string RootSettings = "autosaver_editor";
        internal const string BaseFolderName = "autosaver_editor";
        internal const string BaseResourcePath = "res://addons/autosaver_editor/";
        internal const string SettingsFileName = "settings.ini";
        internal const string PluginVersion = "0.1.0";
        internal const string PluginIcon = "icon_autosaver.png";


        // settings
        internal const string KeyEnabled = "enabled";
        internal const string KeyIntervalSec = "interval";
        internal const string KeyVerbose = "verbose";
        internal const string KeyPostponeTimeSec = "postpone_time";
        internal const string KeyActivityCheckWindowSec = "activity_check";
        internal const string KeyAutosaveScene = "autosave_scene";
        internal const string KeyAutosaveGDScript = "autosave_gdscript";

        internal const string KeyUseGDEditorAutosaveInterval = "use_gd_editor_autosave_interval";
        internal const string KeyUseGDEditorSaveOnFocusLoss = "use_gd_editor_save_on_focus_loss";
    }
}

using System;
using System.Text;
using AutoSaverPlugin.Shared;

namespace AutoSaverPlugin.Contracts;

internal interface IConfigurationManager
{
    event Action<bool> AutoSaverStateChanged;

    string PluginFullName { get; }
    string PluginShortName { get; }
    string PluginVersion { get; }

    string PluginIConResourcePath { get; }
    VerboseLevel VerboseLevelSetting { get; }
    int AutoSaverIntervalSetting { get; }
    int PostponeTimeSetting { get; }
    int ActivityCheckWindowSetting { get; }
    bool IsOptionSaveScenesEnabled { get; }
    bool IsOptionSaveScriptsEnabled { get; }
    bool IsAutoSaverEnabled { get; }

    bool UseGDEditorSaveOnFocusLoss { get; }
    bool UseGDEditorAutosaveIntervalSecs { get; }
    bool GDEditor_save_on_focus_loss { get; }
    int GDEditor_autosave_interval_secs { get; }
    bool HasGDEditorAutosaveEnabled { get; }

    void SetEditorSaveOnFocusLoss(bool enabled);

    void SetEditorAutosaveIntervalSecs(int seconds);

    void LoadSettings();

    void SaveSettings();

    void SetAutoSaverEnabled(bool enabled, bool noEmitSignal = false);

    void SetSaverInterval(int seconds);

    void SetVerboseLevel(VerboseLevel level);

    void SetSceneEnabled(bool enabled);

    void SetScriptEnabled(bool enabled);
}
